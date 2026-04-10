#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
基金数据库管理和更新脚本
支持创建/修复数据库，全量更新和增量更新基金数据
"""

import sys
import os
import re
import json
import sqlite3
import time
import random
import pickle
import logging
import math
import gc
import signal
import functools
from datetime import datetime, timedelta, date
from typing import Optional, Dict, List, Any, Tuple
from concurrent.futures import ThreadPoolExecutor, as_completed
from contextlib import contextmanager

try:
    import numpy as np
    NUMPY_AVAILABLE = True
except ImportError:
    NUMPY_AVAILABLE = False

try:
    import pandas as pd
    PANDAS_AVAILABLE = True
except ImportError:
    PANDAS_AVAILABLE = False

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
CACHE_DIR = os.path.join(SCRIPT_DIR, "cache")
DATABASE_PATH = os.path.join(SCRIPT_DIR, "fund_database.db")

LOG_DIR = os.path.join(SCRIPT_DIR, "logs")
LOG_FILE = os.path.join(LOG_DIR, f"fund_db_manager_{datetime.now().strftime('%Y%m%d')}.log")

if not os.path.exists(CACHE_DIR):
    os.makedirs(CACHE_DIR)
if not os.path.exists(LOG_DIR):
    os.makedirs(LOG_DIR)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s",
    handlers=[
        logging.FileHandler(LOG_FILE, encoding="utf-8"),
        logging.StreamHandler(sys.stdout)
    ]
)
logger = logging.getLogger(__name__)


class FundConfig:
    def __init__(
        self,
        db_path: str = None,
        cache_dir: str = None,
        log_dir: str = None,
        risk_free_rate: float = 0.025,
        annual_trading_days: int = 252
    ):
        self.db_path = db_path or os.path.join(SCRIPT_DIR, "fund_database.db")
        self.cache_dir = cache_dir or os.path.join(SCRIPT_DIR, "cache")
        self.log_dir = log_dir or os.path.join(SCRIPT_DIR, "logs")
        self.risk_free_rate = risk_free_rate
        self.annual_trading_days = annual_trading_days

    def ensure_dirs(self):
        for dir_path in [self.cache_dir, self.log_dir]:
            if not os.path.exists(dir_path):
                os.makedirs(dir_path)


DEFAULT_CONFIG = FundConfig()


AKSHARE_AVAILABLE = False
try:
    import akshare as ak
    AKSHARE_AVAILABLE = True
    logger.info("akshare is available")
except ImportError:
    logger.warning("akshare not installed, using mock data")

CACHE_EXPIRY_LIST = 1 * 3600
CACHE_EXPIRY_BASIC_INFO = 365 * 24 * 3600
CACHE_EXPIRY_HISTORICAL_NAV = 2 * 365 * 24 * 3600
CACHE_EXPIRY_CURRENT_YEAR_NAV = 1 * 3600
CACHE_EXPIRY_DAY = 24 * 3600

PERIOD_TYPES = ["成立以来", "今年以来", "近1周", "近1月", "近3月", "近6月", "近1年", "近3年"]
ALL_PERIOD_TYPES = PERIOD_TYPES + ["历史年份"]

UPDATE_LOCK_FILE = os.path.join(SCRIPT_DIR, ".update_lock")
UPDATE_LOCK_TIMEOUT = 3600

DEFAULT_MAX_WORKERS = min(os.cpu_count() * 2, 10)
MAX_CONCURRENT_WORKERS = 20

_lock_acquired = False


def parse_chinese_date(date_str: Any) -> Optional[datetime]:
    if not date_str:
        return None
    try:
        normalized = str(date_str).replace("年", "-").replace("月", "-").replace("日", "")[:10]
        return datetime.strptime(normalized, "%Y-%m-%d")
    except Exception as e:
        logger.debug(f"Failed to parse date '{date_str}': {e}")
        return None


def build_performance_record(
    fund_code: str,
    period_type: str,
    period_value: str,
    perf: Optional[Dict[str, Any]] = None
) -> Dict[str, Any]:
    base = {
        "代码": fund_code,
        "周期类型": period_type,
        "周期值": period_value,
        "同类型基金排名": None,
        "同类型基金总数": None,
        "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    }
    
    metrics = ["净值增长率", "最大回撤", "下行标准差", "夏普比率", "索提诺比率", "卡玛比率", "年化收益率", "波动率"]
    for metric in metrics:
        base[metric] = perf.get(metric) if perf else None
    
    return base


def _signal_handler(signum, frame):
    global _lock_acquired
    if _lock_acquired:
        logger.warning(f"Received signal {signum}, releasing lock before exit")
        release_update_lock()
    sys.exit(1)


def _register_signal_handlers():
    signal.signal(signal.SIGINT, _signal_handler)
    signal.signal(signal.SIGTERM, _signal_handler)


def _is_lock_stale() -> bool:
    if not os.path.exists(UPDATE_LOCK_FILE):
        return False
    try:
        with open(UPDATE_LOCK_FILE, "r") as f:
            content = f.read().strip()
            if "|" in content:
                _, timestamp_str = content.split("|", 1)
                lock_time = float(timestamp_str)
                if time.time() - lock_time > UPDATE_LOCK_TIMEOUT:
                    return True
    except Exception:
        pass
    return False

MAX_RETRIES = 3
RETRY_BASE_DELAY = 1


def retry_operation(func, max_retries: int = MAX_RETRIES, base_delay: int = RETRY_BASE_DELAY, return_none_on_failure: bool = False):
    for attempt in range(max_retries):
        try:
            return func()
        except Exception as e:
            if attempt == max_retries - 1:
                if return_none_on_failure:
                    logger.warning(f"All {max_retries} attempts failed for {func.__name__ if hasattr(func, '__name__') else 'operation'}: {e}")
                    return None
                raise
            delay = base_delay * (2 ** attempt)
            logger.warning(f"Attempt {attempt + 1} failed: {e}, retrying in {delay}s")
            time.sleep(delay)


def get_cache_key(prefix: str, *args) -> str:
    key_parts = [prefix] + [str(arg) for arg in args]
    return "_".join(key_parts)


def get_cache(file_name: str, use_pickle: bool = True, cache_expiry: int = None) -> Optional[Any]:
    cache_file = os.path.join(CACHE_DIR, file_name)
    if use_pickle:
        pkl_file = cache_file + ".pkl"
        if os.path.exists(pkl_file):
            cache_file = pkl_file
        elif not os.path.exists(cache_file):
            return None
    elif not os.path.exists(cache_file):
        return None

    try:
        mtime = os.path.getmtime(cache_file)
        expiry = cache_expiry if cache_expiry is not None else CACHE_EXPIRY_DAY
        if time.time() - mtime > expiry:
            os.remove(cache_file)
            return None

        if use_pickle and cache_file.endswith(".pkl"):
            with open(cache_file, "rb") as f:
                return pickle.load(f)
        else:
            with open(cache_file, "r", encoding="utf-8") as f:
                return json.load(f)
    except Exception as e:
        logger.error(f"Error reading cache: {e}")
        return None


def set_cache(file_name: str, data: Any, use_pickle: bool = True, cache_expiry: int = None) -> bool:
    cache_file = os.path.join(CACHE_DIR, file_name)
    if use_pickle:
        cache_file = cache_file + ".pkl"
    try:
        if use_pickle and isinstance(data, (dict, list)):
            with open(cache_file, "wb") as f:
                pickle.dump(data, f)
        else:
            with open(cache_file, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
        return True
    except Exception as e:
        logger.error(f"Error writing cache: {e}")
        return False


def clear_cache(file_name: str) -> None:
    for ext in ["", ".pkl", ".json"]:
        cache_file = os.path.join(CACHE_DIR, file_name + ext)
        try:
            if os.path.exists(cache_file):
                os.remove(cache_file)
        except Exception as e:
            logger.error(f"Error clearing cache {cache_file}: {e}")


def clear_expired_cache() -> None:
    try:
        for file_name in os.listdir(CACHE_DIR):
            cache_file = os.path.join(CACHE_DIR, file_name)
            if os.path.isfile(cache_file):
                mtime = os.path.getmtime(cache_file)
                if time.time() - mtime > CACHE_EXPIRY_DAY:
                    os.remove(cache_file)
                    logger.info(f"Removed expired cache: {file_name}")
    except Exception as e:
        logger.error(f"Error clearing cache: {e}")


@contextmanager
def get_db_connection(db_path: str = DATABASE_PATH, optimize: bool = False):
    conn = sqlite3.connect(db_path, timeout=30, check_same_thread=False)
    conn.execute("PRAGMA busy_timeout = 30000")
    conn.row_factory = sqlite3.Row

    if optimize:
        conn.execute("PRAGMA journal_mode=WAL")
        conn.execute("PRAGMA synchronous=NORMAL")
        conn.execute("PRAGMA cache_size=-1048576")
        conn.execute("PRAGMA temp_store=MEMORY")
        conn.execute("PRAGMA mmap_size=268435456")

    try:
        yield conn
    finally:
        conn.close()


def acquire_update_lock(timeout: int = 1800) -> bool:
    global _lock_acquired
    start_time = time.time()

    if _is_lock_stale():
        logger.warning("Removing stale lock file")
        try:
            os.remove(UPDATE_LOCK_FILE)
        except Exception as e:
            logger.error(f"Error removing stale lock: {e}")

    while os.path.exists(UPDATE_LOCK_FILE):
        if time.time() - start_time > timeout:
            logger.error("Update lock timeout")
            return False
        time.sleep(1)
    try:
        with open(UPDATE_LOCK_FILE, "w") as f:
            f.write(f"{os.getpid()}|{time.time()}")
        _lock_acquired = True
        _register_signal_handlers()
        return True
    except Exception as e:
        logger.error(f"Error acquiring lock: {e}")
        return False


def release_update_lock() -> None:
    global _lock_acquired
    try:
        if os.path.exists(UPDATE_LOCK_FILE):
            os.remove(UPDATE_LOCK_FILE)
        _lock_acquired = False
    except Exception as e:
        logger.error(f"Error releasing lock: {e}")


def create_database(db_path: str = DATABASE_PATH) -> bool:
    logger.info(f"Creating database at {db_path}")
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_basic_info (
                    代码 VARCHAR(20) PRIMARY KEY,
                    名称 VARCHAR(100),
                    基金类型 VARCHAR(20),
                    基金子类型 VARCHAR(20),
                    份额类型 VARCHAR(10),
                    主基金代码 VARCHAR(20),
                    成立日期 DATE,
                    上市日期 DATE,
                    基金管理人 VARCHAR(100),
                    基金托管人 VARCHAR(100),
                    管理费率 VARCHAR(20),
                    托管费率 VARCHAR(20),
                    销售服务费率 VARCHAR(20),
                    业绩比较基准 VARCHAR(500),
                    跟踪标的 VARCHAR(100),
                    投资风格 VARCHAR(20),
                    风险等级 VARCHAR(10),
                    更新日期 DATETIME
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_nav_history (
                    代码 VARCHAR(20),
                    日期 DATE,
                    单位净值 DECIMAL(10,4),
                    累计净值 DECIMAL(10,4),
                    日增长率 DECIMAL(8,4),
                    更新日期 DATETIME,
                    PRIMARY KEY (代码, 日期)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_performance (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    代码 VARCHAR(20),
                    周期类型 VARCHAR(20),
                    周期值 VARCHAR(20),
                    净值增长率 DECIMAL(8,4),
                    最大回撤 DECIMAL(8,4),
                    下行标准差 DECIMAL(8,4),
                    夏普比率 DECIMAL(8,4),
                    索提诺比率 DECIMAL(8,4),
                    卡玛比率 DECIMAL(8,4),
                    年化收益率 DECIMAL(8,4),
                    波动率 DECIMAL(8,4),
                    同类型基金排名 INT,
                    同类型基金总数 INT,
                    更新日期 DATETIME,
                    UNIQUE(代码, 周期类型, 周期值)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_manager (
                    代码 VARCHAR(20),
                    基金经理姓名 VARCHAR(50),
                    任职开始日期 DATE,
                    任职结束日期 DATE,
                    管理天数 INT,
                    更新日期 DATETIME,
                    PRIMARY KEY (代码, 基金经理姓名, 任职开始日期)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_corporate_actions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    代码 VARCHAR(20),
                    除权日期 DATE,
                    事件类型 VARCHAR(20),
                    每份分红金额 DECIMAL(10,4),
                    分红发放日 DATE,
                    拆分比例 DECIMAL(10,4),
                    权益登记日 DATE,
                    事件描述 VARCHAR(500),
                    公告日期 DATE,
                    更新日期 DATETIME,
                    UNIQUE(代码, 除权日期, 事件类型)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_asset_scale (
                    代码 VARCHAR(20),
                    日期 DATE,
                    资产规模 DECIMAL(12,2),
                    份额规模 DECIMAL(12,2),
                    更新日期 DATETIME,
                    PRIMARY KEY (代码, 日期)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_purchase_status (
                    代码 VARCHAR(20),
                    日期 DATE,
                    申购状态 VARCHAR(20),
                    申购限额 DECIMAL(12,2),
                    申购手续费率 DECIMAL(5,4),
                    更新日期 DATETIME,
                    PRIMARY KEY (代码, 日期)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_redemption_status (
                    代码 VARCHAR(20),
                    日期 DATE,
                    赎回状态 VARCHAR(20),
                    赎回限额 DECIMAL(12,2),
                    赎回手续费率 DECIMAL(5,4),
                    更新日期 DATETIME,
                    PRIMARY KEY (代码, 日期)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS user_favorite_funds (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    用户ID VARCHAR(50),
                    基金代码 VARCHAR(20),
                    添加时间 DATETIME,
                    排序序号 INT,
                    备注 VARCHAR(500),
                    分组标签 VARCHAR(50),
                    提醒设置 VARCHAR(200),
                    更新日期 DATETIME,
                    UNIQUE(用户ID, 基金代码)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS user_favorite_scores (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    用户ID VARCHAR(50),
                    基金代码 VARCHAR(20),
                    评分日期 DATE,
                    综合评分 DECIMAL(5,2),
                    收益因子得分 DECIMAL(5,2),
                    风险因子得分 DECIMAL(5,2),
                    风险调整收益得分 DECIMAL(5,2),
                    排名因子得分 DECIMAL(5,2),
                    评分排名 INT,
                    评分变化 DECIMAL(5,2),
                    评分趋势 VARCHAR(10),
                    权重配置 VARCHAR(500),
                    计算时间 DATETIME,
                    更新日期 DATETIME,
                    UNIQUE(用户ID, 基金代码, 评分日期)
                )
            """)

            cursor.execute("""
                CREATE TABLE IF NOT EXISTS fund_info_update_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    基金代码列表 VARCHAR(5000),
                    更新类型 VARCHAR(20),
                    更新表名 VARCHAR(30),
                    更新状态 VARCHAR(20),
                    更新记录数 INT,
                    新增记录数 INT,
                    更新耗时 INT,
                    错误信息 VARCHAR(5000),
                    更新来源 VARCHAR(50),
                    操作人 VARCHAR(50),
                    更新日期 DATETIME,
                    UNIQUE(基金代码列表, 更新日期, 更新类型)
                )
            """)

            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_basic_info_type ON fund_basic_info(基金类型)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_basic_info_manager ON fund_basic_info(基金管理人)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_basic_info_main_code ON fund_basic_info(主基金代码)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_nav_history_code_date ON fund_nav_history(代码, 日期)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_nav_history_date ON fund_nav_history(日期)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_corporate_actions_code_date ON fund_corporate_actions(代码, 除权日期)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_corporate_actions_type ON fund_corporate_actions(事件类型)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_user_favorite_funds_user_fund ON user_favorite_funds(用户ID, 基金代码)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_user_favorite_funds_user ON user_favorite_funds(用户ID)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_user_favorite_scores_user_fund_date ON user_favorite_scores(用户ID, 基金代码, 评分日期)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_user_favorite_scores_user ON user_favorite_scores(用户ID)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_performance_code_period ON fund_performance(代码, 周期类型)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_performance_period_code ON fund_performance(周期类型, 代码, 净值增长率)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_manager_code ON fund_manager(代码)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_manager_name_date ON fund_manager(基金经理姓名, 任职开始日期)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_asset_scale_code_date ON fund_asset_scale(代码, 日期)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_purchase_status_code_date ON fund_purchase_status(代码, 日期)")
            cursor.execute("CREATE INDEX IF NOT EXISTS idx_fund_redemption_status_code_date ON fund_redemption_status(代码, 日期)")

            conn.commit()
            logger.info("Database created successfully")
            return True
    except Exception as e:
        logger.error(f"Error creating database: {e}")
        return False


def repair_database(db_path: str = DATABASE_PATH) -> bool:
    logger.info(f"Repairing database at {db_path}")
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()

            cursor.execute("PRAGMA integrity_check")
            result = cursor.fetchone()
            if result[0] != "ok":
                logger.warning(f"Integrity check result: {result[0]}")

            cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
            tables = [row[0] for row in cursor.fetchall()]

            required_tables = [
                "fund_basic_info", "fund_nav_history", "fund_performance",
                "fund_manager", "fund_corporate_actions", "fund_asset_scale",
                "fund_purchase_status", "fund_redemption_status",
                "user_favorite_funds", "user_favorite_scores",
                "fund_info_update_history"
            ]

            for table in required_tables:
                if table not in tables:
                    logger.warning(f"Missing table: {table}, creating...")
                    cursor.execute(f"CREATE TABLE IF NOT EXISTS {table} (id INTEGER PRIMARY KEY)")

            cursor.execute("PRAGMA foreign_key_check")
            fk_issues = cursor.fetchall()
            if fk_issues:
                logger.warning(f"Foreign key issues found: {fk_issues}")

            conn.commit()
            logger.info("Database repair completed")
            return True
    except Exception as e:
        logger.error(f"Error repairing database: {e}")
        return False


def get_all_fund_codes_fromDb(db_path: str = DATABASE_PATH) -> List[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 代码 FROM fund_basic_info")
            return [row[0] for row in cursor.fetchall()]
    except Exception as e:
        logger.error(f"Error getting fund codes: {e}")
        return []


@functools.lru_cache(maxsize=5000)
def _cached_get_establishment_date(fund_code: str, db_path: str) -> Optional[datetime]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 成立日期 FROM fund_basic_info WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            if not result or not result[0]:
                return None
            return parse_chinese_date(result[0])
    except Exception as e:
        logger.error(f"Error getting fund establishment date: {e}")
        return None


def get_fund_establishment_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[datetime]:
    return _cached_get_establishment_date(fund_code, db_path)


def get_latest_update_history(db_path: str = DATABASE_PATH) -> Optional[Dict]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT * FROM fund_info_update_history ORDER BY 更新日期 DESC LIMIT 1"
            )
            row = cursor.fetchone()
            if row:
                return dict(row)
            return None
    except Exception as e:
        logger.error(f"Error getting latest update history: {e}")
        return None


def fetch_fund_list_from_akshare(use_cache: bool = True, limit: int = None, only_a_share: bool = False) -> List[str]:
    cache_key = "fund_list_all" if not only_a_share else "fund_list_a_share"

    if use_cache:
        cached_data = get_cache(cache_key, use_pickle=True)
        if cached_data:
            logger.info(f"Using cached fund list with {len(cached_data)} funds")
            return cached_data[:limit] if limit else cached_data

    if not AKSHARE_AVAILABLE:
        logger.warning("akshare not available, using default fund list")
        default_list = ["008528", "001438", "025209"]
        return default_list[:limit] if limit else default_list

    try:
        time.sleep(random.uniform(0.1, 0.3))
        fund_df = ak.fund_open_fund_daily_em()
        fund_codes = fund_df["基金代码"].tolist()
        logger.info(f"Fetched {len(fund_codes)} fund codes from akshare")

        if only_a_share and "基金简称" in fund_df.columns:
            fund_names = fund_df["基金简称"].tolist()
            filtered = []
            for code, name in zip(fund_codes, fund_names):
                if determine_share_type(code, name) == "A":
                    filtered.append(code)
            fund_codes = filtered
            logger.info(f"Filtered to {len(fund_codes)} A-share funds")

        if fund_codes:
            set_cache(cache_key, fund_codes, use_pickle=True, cache_expiry=CACHE_EXPIRY_LIST)

        return fund_codes[:limit] if limit else fund_codes
    except Exception as e:
        logger.error(f"Error fetching fund list: {e}")
        cached_data = get_cache(cache_key, use_pickle=True, cache_expiry=CACHE_EXPIRY_LIST)
        if cached_data:
            logger.info(f"Using cached fund list after error, {len(cached_data)} funds")
            return cached_data[:limit] if limit else cached_data
        default_list = ["008528", "001438", "025209"]
        return default_list[:limit] if limit else default_list


def determine_share_type(fund_code: str, fund_name: str = None) -> str:
    if not fund_name:
        return "A"
    last_char = fund_name[-1] if fund_name else None
    if last_char and last_char.isupper() and last_char.isalpha():
        return last_char
    return "A"


def parse_fund_type(fund_type_raw: str) -> Tuple[str, str]:
    if not fund_type_raw:
        return "", ""
    for sep in ["-", "–", "—", "_"]:
        if sep in fund_type_raw:
            parts = fund_type_raw.split(sep, 1)
            return parts[0].strip(), parts[1].strip()
    return fund_type_raw.strip(), ""


def find_main_fund_code(fund_name: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    if not fund_name:
        return None
    main_name = fund_name[:-1] + "A" if fund_name and fund_name[-1].isupper() else None
    if not main_name:
        return None
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 代码 FROM fund_basic_info WHERE 名称 = ?", (main_name,))
            result = cursor.fetchone()
            return result[0] if result else None
    except Exception as e:
        logger.error(f"Error finding main fund code: {e}")
        return None


def fetch_fund_basic_info(fund_code: str, db_path: str = DATABASE_PATH, use_cache: bool = True) -> Optional[Dict[str, Any]]:
    cache_key = get_cache_key("basic", fund_code)
    if use_cache:
        cached_data = get_cache(cache_key, cache_expiry=CACHE_EXPIRY_BASIC_INFO)
        if cached_data:
            return cached_data

    if not AKSHARE_AVAILABLE:
        logger.warning("akshare not available, cannot fetch basic info")
        return None

    try:
        time.sleep(random.uniform(0.05, 0.15))
        fund_overview = ak.fund_overview_em(symbol=fund_code)
        if fund_overview.empty:
            return None

        row = fund_overview.iloc[0]
        fund_name = row.get("基金简称", "")
        share_type = determine_share_type(fund_code, fund_name)

        fund_type, sub_type = parse_fund_type(row.get("基金类型", ""))

        asset_str = row.get("净资产规模", "")
        share_str = row.get("份额规模", "")
        asset_scale = None
        share_scale = None

        if "亿元" in asset_str:
            match = re.search(r"([\d.]+)亿元", asset_str)
            if match:
                asset_scale = float(match.group(1))

        if "亿份" in share_str:
            match = re.search(r"([\d.]+)亿份", share_str)
            if match:
                share_scale = float(match.group(1))

        data = {
            "代码": fund_code,
            "名称": fund_name,
            "基金类型": fund_type,
            "基金子类型": sub_type,
            "份额类型": share_type,
            "成立日期": row.get("成立日期/规模", "2020-01-01").split(" / ")[0] if "成立日期/规模" in row else "2020-01-01",
            "上市日期": None,
            "基金管理人": row.get("基金管理人", ""),
            "基金经理人": row.get("基金经理人", ""),
            "基金托管人": row.get("基金托管人", ""),
            "管理费率": row.get("管理费率", "1.5%"),
            "托管费率": row.get("托管费率", "0.2%"),
            "销售服务费率": row.get("销售服务费率", ""),
            "业绩比较基准": row.get("业绩比较基准", ""),
            "跟踪标的": row.get("跟踪标的", ""),
            "投资风格": row.get("投资风格", ""),
            "风险等级": row.get("风险等级", ""),
            "资产规模": asset_scale,
            "份额规模": share_scale,
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        }

        if share_type and share_type != "A":
            main_fund_code = find_main_fund_code(fund_name, db_path)
            data["主基金代码"] = main_fund_code

        set_cache(cache_key, data, cache_expiry=CACHE_EXPIRY_BASIC_INFO)
        return data
    except Exception as e:
        logger.error(f"Error fetching basic info for {fund_code}: {e}")
        return None


def fetch_fund_nav_history(fund_code: str, start_date: str, end_date: str, fund_start_date: datetime = None, use_cache: bool = True, db_path: str = DATABASE_PATH) -> List[Dict[str, Any]]:
    if not AKSHARE_AVAILABLE:
        logger.warning("akshare not available, using mock nav history")
        return generate_mock_nav_history(fund_code, start_date, end_date)

    try:
        all_data = fetch_full_nav_history(fund_code, use_cache)

        if not all_data:
            return generate_mock_nav_history(fund_code, start_date, end_date)

        filtered_data = all_data
        if start_date and end_date:
            filtered_data = [item for item in all_data if start_date <= item["日期"] <= end_date]
        elif start_date:
            filtered_data = [item for item in all_data if item["日期"] >= start_date]
        elif end_date:
            filtered_data = [item for item in all_data if item["日期"] <= end_date]

        return filtered_data
    except Exception as e:
        logger.error(f"Error fetching nav history for {fund_code}: {e}")
        return generate_mock_nav_history(fund_code, start_date, end_date)


def fetch_full_nav_history(fund_code: str, use_cache: bool = True) -> List[Dict[str, Any]]:
    cache_key = get_cache_key("nav_full", fund_code)
    cache_expiry = CACHE_EXPIRY_CURRENT_YEAR_NAV

    if use_cache:
        cached_data = get_cache(cache_key, use_pickle=True, cache_expiry=cache_expiry)
        if cached_data:
            return cached_data

    try:
        time.sleep(random.uniform(0.1, 0.2))
        nav_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="单位净值走势")
        time.sleep(random.uniform(0.1, 0.2))
        accum_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="累计净值走势")

        if nav_df.empty or accum_df.empty:
            return []

        nav_df = nav_df.rename(columns={"净值日期": "date", "单位净值": "nav", "日增长率": "daily_return"})
        accum_df = accum_df.rename(columns={"净值日期": "date", "累计净值": "accumulated_nav"})

        df = pd.merge(nav_df, accum_df, on="date", how="inner")
        df["date"] = pd.to_datetime(df["date"])
        df = df.sort_values("date").reset_index(drop=True)

        if df.empty:
            return []

        df["daily_return"] = df["daily_return"].fillna(0)

        # akshare返回的日增长率是*100的结果
        df["daily_return"] = df["daily_return"] / 100

        full_data = []
        for _, row in df.iterrows():
            daily_return = row["daily_return"]
            full_data.append({
                "代码": fund_code,
                "日期": row["date"].strftime("%Y-%m-%d"),
                "单位净值": round(row["nav"], 4),
                "累计净值": round(row["accumulated_nav"], 4),
                "日增长率": round(daily_return, 6) if not pd.isna(daily_return) else 0,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            })

        if full_data:
            set_cache(cache_key, full_data, use_pickle=True, cache_expiry=cache_expiry)

        return full_data
    except Exception as e:
        logger.error(f"Error fetching full nav for {fund_code}: {e}")
        return []


def fetch_nav_for_year(fund_code: str, year: int, use_cache: bool = True) -> List[Dict[str, Any]]:
    cache_key = get_cache_key("nav_year", fund_code, str(year))
    cache_expiry = CACHE_EXPIRY_CURRENT_YEAR_NAV if year == datetime.now().year else CACHE_EXPIRY_HISTORICAL_NAV

    if use_cache:
        cached_data = get_cache(cache_key, use_pickle=True, cache_expiry=cache_expiry)
        if cached_data:
            return cached_data

    full_cache_key = get_cache_key("nav_full", fund_code)
    full_cached = get_cache(full_cache_key, use_pickle=True, cache_expiry=CACHE_EXPIRY_CURRENT_YEAR_NAV)
    if full_cached:
        year_data = [item for item in full_cached if item["日期"].startswith(str(year))]
        if year_data:
            set_cache(cache_key, year_data, use_pickle=True, cache_expiry=cache_expiry)
            return year_data

    try:
        time.sleep(random.uniform(0.1, 0.2))
        nav_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="单位净值走势")
        time.sleep(random.uniform(0.1, 0.2))
        accum_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="累计净值走势")

        if nav_df.empty or accum_df.empty:
            return []

        nav_df = nav_df.rename(columns={"净值日期": "date", "单位净值": "nav", "日增长率": "daily_return"})
        accum_df = accum_df.rename(columns={"净值日期": "date", "累计净值": "accumulated_nav"})

        df = pd.merge(nav_df, accum_df, on="date", how="inner")
        df["date"] = pd.to_datetime(df["date"])
        if year is not None:
            df = df[(df["date"] >= pd.to_datetime(f"{year}-01-01")) & (df["date"] <= pd.to_datetime(f"{year}-12-31"))]
        df = df.sort_values("date").reset_index(drop=True)

        if df.empty:
            return []

        df["daily_return"] = df["daily_return"].fillna(0)

        # akshare返回的日增长率是*100的结果
        df["daily_return"] = df["daily_return"] / 100

        year_data = []
        for _, row in df.iterrows():
            daily_return = row["daily_return"]
            year_data.append({
                "代码": fund_code,
                "日期": row["date"].strftime("%Y-%m-%d"),
                "单位净值": round(row["nav"], 4),
                "累计净值": round(row["accumulated_nav"], 4),
                "日增长率": round(daily_return, 6) if not pd.isna(daily_return) else 0,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            })

        if year_data:
            cache_expiry = CACHE_EXPIRY_CURRENT_YEAR_NAV if year == datetime.now().year else CACHE_EXPIRY_HISTORICAL_NAV
            set_cache(cache_key, year_data, use_pickle=True, cache_expiry=cache_expiry)

        return year_data
    except Exception as e:
        logger.error(f"Error fetching nav for {fund_code} year {year}: {e}")
        return []


def generate_mock_nav_history(fund_code: str, start_date: str, end_date: str) -> List[Dict[str, Any]]:
    try:
        if end_date is None:
            end = datetime.now()
        else:
            end = datetime.strptime(end_date, "%Y-%m-%d")

        if start_date is None:
            start = end - timedelta(days=365)
        else:
            start = datetime.strptime(start_date, "%Y-%m-%d")

        days = (end - start).days + 1
        if days <= 0:
            days = 1

        data = []
        nav = 1.0
        for i in range(days):
            date_obj = start + timedelta(days=i)
            daily_growth = random.uniform(-0.03, 0.05)
            nav *= 1 + daily_growth
            accumulated_nav = nav * (1 + random.uniform(0, 0.5))

            data.append({
                "代码": fund_code,
                "日期": date_obj.strftime("%Y-%m-%d"),
                "单位净值": round(nav, 4),
                "累计净值": round(accumulated_nav, 4),
                "日增长率": round(daily_growth * 100, 4),
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            })
        return data
    except Exception as e:
        logger.error(f"Error generating mock nav history: {e}")
        return []


def fetch_corporate_actions(fund_code: str, start_date: str = None, end_date: str = None) -> List[Dict[str, Any]]:
    cache_key = get_cache_key("corporate_actions", fund_code)
    cached_data = get_cache(cache_key)
    if cached_data:
        return cached_data

    if not AKSHARE_AVAILABLE:
        logger.warning("akshare not available, using mock corporate actions")
        return generate_mock_corporate_actions(fund_code)

    try:
        time.sleep(random.uniform(0.05, 0.1))
        actions_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="分红送配详情")
        if actions_df.empty:
            return generate_mock_corporate_actions(fund_code)

        data = []
        for _, row in actions_df.iterrows():
            ex_date = str(row.get("除权除息日", ""))
            if start_date and ex_date < start_date:
                continue
            if end_date and ex_date > end_date:
                continue
            event_type = "DIVIDEND" if "分红" in str(row.get("方案", "")) else "SPLIT"
            dividend_per_share = float(row.get("每份分红(元)", 0)) if event_type == "DIVIDEND" else None
            payment_date = str(row.get("发放日", "")) if row.get("发放日", "") else None
            split_ratio = float(row.get("拆分比例", 1)) if event_type == "SPLIT" else None
            record_date = str(row.get("权益登记日", "")) if row.get("权益登记日", "") else None
            event_description = str(row.get("方案", ""))
            announcement_date = str(row.get("公告日", "")) if row.get("公告日", "") else None

            data.append({
                "代码": fund_code,
                "除权日期": ex_date,
                "事件类型": event_type,
                "每份分红金额": round(dividend_per_share, 4) if dividend_per_share else None,
                "分红发放日": payment_date,
                "拆分比例": round(split_ratio, 4) if split_ratio else None,
                "权益登记日": record_date,
                "事件描述": event_description,
                "公告日期": announcement_date,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            })

        if data:
            set_cache(cache_key, data)
        return data
    except Exception as e:
        logger.error(f"Error fetching corporate actions for {fund_code}: {e}")
        return generate_mock_corporate_actions(fund_code)


def generate_mock_corporate_actions(fund_code: str) -> List[Dict[str, Any]]:
    event_types = ["DIVIDEND", "SPLIT"]
    data = []
    today = datetime.now()
    for i in range(5):
        ex_date = (today - timedelta(days=i * 180)).strftime("%Y-%m-%d")
        event_type = random.choice(event_types)
        dividend_per_share = random.uniform(0.01, 0.5) if event_type == "DIVIDEND" else None
        payment_date = (datetime.strptime(ex_date, "%Y-%m-%d") + timedelta(days=3)).strftime("%Y-%m-%d") if event_type == "DIVIDEND" else None
        split_ratio = random.uniform(1.1, 2.0) if event_type == "SPLIT" else None
        record_date = (datetime.strptime(ex_date, "%Y-%m-%d") - timedelta(days=1)).strftime("%Y-%m-%d")
        announcement_date = (datetime.strptime(ex_date, "%Y-%m-%d") - timedelta(days=7)).strftime("%Y-%m-%d")
        event_description = f"{event_type} event" if event_type == "SPLIT" else f"分红 {dividend_per_share:.4f} 元/份"

        data.append({
            "代码": fund_code,
            "除权日期": ex_date,
            "事件类型": event_type,
            "每份分红金额": round(dividend_per_share, 4) if dividend_per_share else None,
            "分红发放日": payment_date,
            "拆分比例": round(split_ratio, 4) if split_ratio else None,
            "权益登记日": record_date,
            "事件描述": event_description,
            "公告日期": announcement_date,
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })
    return data


def get_nav_history_from_db(
    fund_code: str, 
    db_path: str = DATABASE_PATH,
    start_date: str = None,
    end_date: str = None
) -> List[Dict[str, Any]]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            sql = """
                SELECT 代码, 日期, 单位净值, 累计净值, 日增长率
                FROM fund_nav_history
                WHERE 代码 = ?
            """
            params = [fund_code]
            
            if start_date:
                sql += " AND 日期 >= ?"
                params.append(start_date)
            if end_date:
                sql += " AND 日期 <= ?"
                params.append(end_date)
            
            sql += " ORDER BY 日期"
            
            cursor.execute(sql, params)
            rows = cursor.fetchall()
            return [dict(row) for row in rows]
    except Exception as e:
        logger.error(f"Error getting nav history from db: {e}")
        return []


def convert_nav_to_dataframe(nav_data: List[Dict[str, Any]]) -> Optional[pd.DataFrame]:
    if not nav_data:
        return None

    df = pd.DataFrame(nav_data)
    df["日期"] = pd.to_datetime(df["日期"])
    df = df.sort_values("日期").reset_index(drop=True)

    if "日增长率" not in df.columns or df["日增长率"].isna().all():
        df["日增长率"] = df["累计净值"].pct_change()

    df["日增长率"] = df["日增长率"].fillna(0)
    if df["日增长率"].dtype == object:
        df["日增长率"] = df["日增长率"].apply(
            lambda x: float(str(x).replace("%", "")) / 100 if pd.notna(x) else 0
        )
    elif df["日增长率"].abs().max() > 1:
        df["日增长率"] = df["日增长率"] / 100

    return df


def get_period_days_map() -> Dict[str, int]:
    return {
        "近1周": 7,
        "近1月": 30,
        "近3月": 89,
        "近6月": 180,
        "近1年": 365,
        "近3年": 1095
    }


def get_period_months_map() -> Dict[str, int]:
    return {
        "近1月": 1,
        "近6月": 6,
    }


def get_date_months_ago(date_obj: datetime, months: int) -> datetime:
    month = date_obj.month - months
    year = date_obj.year
    while month <= 0:
        month += 12
        year -= 1
    day = min(date_obj.day, [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1])
    return datetime(year, month, day)


def filter_period_data(
    df: pd.DataFrame,
    period_type: str,
    period_value: str = ""
) -> Optional[pd.DataFrame]:
    if df is None or len(df) < 2:
        return None

    today = df["日期"].max()
    period_days_map = get_period_days_map()
    period_months_map = get_period_months_map()

    if period_type == "成立以来":
        period_df = df
    elif period_type == "今年以来":
        year_start = datetime(today.year, 1, 1)
        period_df = df[df["日期"] >= year_start]
        if len(period_df) < 2:
            return None
    elif period_type in period_months_map:
        months = period_months_map[period_type]
        start_date = get_date_months_ago(today, months)
        period_df = df[df["日期"] >= start_date]
        if len(period_df) < 2:
            return None
    elif period_type in period_days_map:
        days = period_days_map[period_type]
        start_date = today - timedelta(days=days)
        if period_type in ["近1周", "近1年"]:
            period_df = df[df["日期"] > start_date]
        else:
            period_df = df[df["日期"] >= start_date]
        if len(period_df) < 2:
            return None
    elif "历史年份" == period_type:
        try:
            year = int(period_value) if period_value else int(period_type.replace("年", ""))
            year_start = datetime(year, 1, 1)
            year_end = datetime(year, 12, 31)
            period_df = df[(df["日期"] >= year_start) & (df["日期"] <= year_end)]
            if len(period_df) < 2:
                return None
        except (ValueError, AttributeError):
            return None
    else:
        return None

    return period_df


def calculate_metrics(
    period_returns: pd.Series,
    period_type: str,
    risk_free_rate: float = None,
    annual_trading_days: int = None
) -> Optional[Dict[str, Any]]:
    if period_returns is None or len(period_returns) < 1:
        return None

    risk_free_rate = risk_free_rate if risk_free_rate is not None else DEFAULT_CONFIG.risk_free_rate
    annual_trading_days = annual_trading_days if annual_trading_days is not None else DEFAULT_CONFIG.annual_trading_days

    cum_return = (1 + period_returns).cumprod()
    period_return = (cum_return.iloc[-1] - 1) * 100

    period_days_map = get_period_days_map()
    if period_type in period_days_map:
        period_days = period_days_map[period_type]
        actual_trading_days = len(period_returns)
        days = min(period_days, actual_trading_days)
    else:
        days = len(period_returns)

    annual_factor = annual_trading_days / max(days, 1)
    annual_return = ((1 + period_return / 100) ** annual_factor - 1) * 100

    annual_vol = period_returns.std() * math.sqrt(annual_trading_days) if not period_returns.empty else 0

    running_max = cum_return.expanding().max()
    drawdown = (cum_return - running_max) / running_max
    max_dd = drawdown.min() if not drawdown.empty else 0

    downside_returns = period_returns[period_returns < 0]
    downside_std = downside_returns.std() * math.sqrt(annual_trading_days) if len(downside_returns) > 1 else 0

    sortino = (annual_return / 100 - risk_free_rate) / downside_std if downside_std > 0 else 0
    calmar = (annual_return / 100) / abs(max_dd) if max_dd != 0 else 0
    sharpe = (annual_return / 100 - risk_free_rate) / annual_vol if annual_vol > 0 else 0

    return {
        "净值增长率": round(period_return, 4),
        "年化收益率": round(annual_return, 4),
        "最大回撤": round(max_dd * 100, 4),
        "下行标准差": round(downside_std * 100, 4),
        "夏普比率": round(sharpe, 4),
        "索提诺比率": round(sortino, 4),
        "卡玛比率": round(calmar, 4),
        "波动率": round(annual_vol * 100, 4)
    }


def calculate_period_performance(
    nav_data: List[Dict[str, Any]],
    period_type: str,
    period_value: str = "",
    risk_free_rate: float = 0.025
) -> Optional[Dict[str, Any]]:
    df = convert_nav_to_dataframe(nav_data)
    if df is None:
        return None

    return calculate_period_performance_from_df(df, period_type, period_value, risk_free_rate)


def calculate_period_performance_from_df(
    df: pd.DataFrame,
    period_type: str,
    period_value: str = "",
    risk_free_rate: float = 0.025
) -> Optional[Dict[str, Any]]:
    returns = df["日增长率"].dropna()
    if len(returns) < 2:
        return None

    period_df = filter_period_data(df, period_type, period_value)
    if period_df is None:
        return None

    period_returns = period_df["日增长率"].dropna()
    if len(period_returns) < 1:
        return None

    return calculate_metrics(period_returns, period_type, risk_free_rate)


def fetch_fund_performance(
    fund_code: str,
    db_path: str = DATABASE_PATH,
    is_first_insert: bool = False,
    fund_start_date: datetime = None,
    use_cache: bool = True,
    nav_data: List[Dict[str, Any]] = None
) -> List[Dict[str, Any]]:
    cache_suffix = "_full" if is_first_insert else "_incremental"
    cache_key = get_cache_key("performance" + cache_suffix, fund_code)
    if use_cache:
        cached_data = get_cache(cache_key)
        if cached_data:
            return cached_data

    if nav_data is None:
        nav_data = get_nav_history_from_db(fund_code, db_path)

    if not nav_data or len(nav_data) < 2:
        return generate_mock_performance(fund_code)

    df = convert_nav_to_dataframe(nav_data)
    if df is None:
        return generate_mock_performance(fund_code)

    current_year = datetime.now().year

    if not fund_start_date:
        fund_start_date = get_fund_establishment_date(fund_code, db_path)
    fund_start_year = fund_start_date.year if fund_start_date else df["日期"].min().year

    period_types = PERIOD_TYPES.copy()

    if is_first_insert:
        start_year = fund_start_year
        historical_years = [str(year) for year in range(current_year - 1, start_year - 1, -1)]
        period_types.extend([("历史年份", year) for year in historical_years])
    else:
        try:
            with get_db_connection(db_path) as conn:
                cursor = conn.cursor()
                cursor.execute("""
                    SELECT 周期类型, 周期值
                    FROM fund_performance
                    WHERE 代码 = ?
                """, (fund_code,))
                existing_periods = cursor.fetchall()
        except sqlite3.OperationalError as e:
            logger.error(f"Database operational error for fund {fund_code}: {e}")
            existing_periods = []
        except sqlite3.IntegrityError as e:
            logger.error(f"Database integrity error for fund {fund_code}: {e}")
            existing_periods = []
        except Exception as e:
            logger.error(f"Unexpected error querying performance for fund {fund_code}: {e}")
            existing_periods = []
        
        existing_years = set(p[1] for p in existing_periods if p[0] == "历史年份" and p[1])
        start_year = fund_start_year
        for year in range(current_year - 1, start_year - 1, -1):
            year_str = str(year)
            if year_str not in existing_years:
                period_types.append(("历史年份", year_str))

    data = []
    for period_type_item in period_types:
        if isinstance(period_type_item, tuple):
            period_type, period_value = period_type_item
        else:
            period_type = period_type_item
            period_value = ""
        perf = calculate_period_performance_from_df(df, period_type, period_value)
        data.append(build_performance_record(fund_code, period_type, period_value, perf))

    if data:
        set_cache(cache_key, data)
    return data


def generate_mock_performance(fund_code: str) -> List[Dict[str, Any]]:
    period_types = PERIOD_TYPES.copy()
    data = []
    for period_type in period_types:
        data.append({
            "代码": fund_code,
            "周期类型": period_type,
            "周期值": period_type,
            "净值增长率": round(random.uniform(-30, 50), 4),
            "最大回撤": round(random.uniform(-30, 0), 4),
            "下行标准差": round(random.uniform(0, 20), 4),
            "夏普比率": round(random.uniform(-1, 3), 4),
            "索提诺比率": round(random.uniform(-1, 3), 4),
            "卡玛比率": round(random.uniform(-2, 5), 4),
            "年化收益率": round(random.uniform(-20, 40), 4),
            "波动率": round(random.uniform(5, 30), 4),
            "同类型基金排名": random.randint(1, 1000),
            "同类型基金总数": random.randint(1000, 5000),
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })
    return data


def fetch_fund_managers(fund_code: str, start_date: str = None, end_date: str = None) -> List[Dict[str, Any]]:
    cache_key = get_cache_key("managers", fund_code)
    cached_data = get_cache(cache_key)
    if cached_data:
        return cached_data

    basic_info = fetch_fund_basic_info(fund_code)
    if not basic_info:
        return []

    manager_names = basic_info.get("基金经理人", "")
    if not manager_names:
        return []

    data = []
    for name in manager_names.split("、"):
        name = name.strip()
        if not name:
            continue
        data.append({
            "代码": fund_code,
            "基金经理姓名": name,
            "任职开始日期": None,
            "任职结束日期": None,
            "管理天数": None,
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })

    if data:
        set_cache(cache_key, data)
    return data


def generate_mock_managers(fund_code: str) -> List[Dict[str, Any]]:
    return [{
        "代码": fund_code,
        "基金经理姓名": "张经理",
        "任职开始日期": "2020-01-01",
        "任职结束日期": None,
        "管理天数": 1500,
        "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    }]


def fetch_asset_scale(fund_code: str, start_date: str = None, end_date: str = None) -> List[Dict[str, Any]]:
    cache_key = get_cache_key("asset_scale", fund_code)
    cached_data = get_cache(cache_key)
    if cached_data:
        return cached_data

    basic_info = fetch_fund_basic_info(fund_code)
    if not basic_info:
        return []

    asset_scale = basic_info.get("资产规模")
    share_scale = basic_info.get("份额规模")

    if asset_scale is None and share_scale is None:
        return []

    today = datetime.now().strftime("%Y-%m-%d")
    data = [{
        "代码": fund_code,
        "日期": today,
        "资产规模": asset_scale,
        "份额规模": share_scale,
        "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    }]

    if data:
        set_cache(cache_key, data)
    return data


def generate_mock_asset_scale(fund_code: str) -> List[Dict[str, Any]]:
    data = []
    today = datetime.now()
    for i in range(12):
        date_str = (today - timedelta(days=i * 30)).strftime("%Y-%m-%d")
        data.append({
            "代码": fund_code,
            "日期": date_str,
            "资产规模": round(random.uniform(1, 100), 2),
            "份额规模": round(random.uniform(1, 80), 2),
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        })
    return data


def _fetch_purchase_em_data(fund_code: str) -> Optional[Dict[str, Any]]:
    cache_key = get_cache_key("purchase_em", fund_code)
    cached_data = get_cache(cache_key)
    if cached_data is not None:
        return cached_data

    if not AKSHARE_AVAILABLE:
        logger.warning("akshare not available, cannot fetch purchase/em data")
        return None

    try:
        time.sleep(random.uniform(0.1, 0.2))
        df = ak.fund_purchase_em()

        fund_data = df[df["基金代码"] == fund_code]
        if fund_data.empty:
            logger.warning(f"No purchase/em data found for fund {fund_code}")
            return None

        record = fund_data.iloc[0]
        result = {
            "申购状态": record["申购状态"],
            "申购限额": record["日累计限定金额"] if record["日累计限定金额"] > 0 else None,
            "申购手续费率": record["手续费"],
            "赎回状态": record["赎回状态"],
            "赎回手续费率": None,
            "赎回限额": None,
        }

        set_cache(cache_key, result)
        return result
    except Exception as e:
        logger.error(f"Error fetching purchase/em data for {fund_code}: {e}")
        return None


def _fetch_redemption_fee_data(fund_code: str) -> Optional[Dict[str, Any]]:
    cache_key = get_cache_key("redemption_fee", fund_code)
    cached_data = get_cache(cache_key)
    if cached_data is not None:
        return cached_data

    if not AKSHARE_AVAILABLE:
        return None

    try:
        time.sleep(random.uniform(0.05, 0.1))
        df = ak.fund_fee_em(symbol=fund_code, indicator="赎回费率")
        if df is None or df.empty:
            return None

        fee_rates = []
        for _, row in df.iterrows():
            fee_str = str(row["赎回费率"]).replace("%", "").strip()
            try:
                fee_rates.append(float(fee_str))
            except ValueError:
                pass

        avg_fee = sum(fee_rates) / len(fee_rates) if fee_rates else None

        df_limit = ak.fund_fee_em(symbol=fund_code, indicator="申购与赎回金额")
        redemption_limit = None
        if df_limit is not None and not df_limit.empty:
            for _, row in df_limit.iterrows():
                if "最小赎回份额" in str(row[0]):
                    limit_str = str(row[1]).replace("份", "").strip()
                    try:
                        redemption_limit = float(limit_str)
                    except ValueError:
                        pass
                    break

        result = {
            "赎回手续费率": avg_fee,
            "赎回限额": redemption_limit,
        }

        set_cache(cache_key, result)
        return result
    except Exception as e:
        logger.error(f"Error fetching redemption fee data for {fund_code}: {e}")
        return None


def fetch_purchase_status(fund_code: str, start_date: str = None, end_date: str = None) -> List[Dict[str, Any]]:
    raw_data = _fetch_purchase_em_data(fund_code)
    if raw_data is None:
        return []

    today = datetime.now().strftime("%Y-%m-%d")
    data = [{
        "代码": fund_code,
        "日期": today,
        "申购状态": raw_data["申购状态"],
        "申购限额": raw_data["申购限额"],
        "申购手续费率": raw_data["申购手续费率"],
        "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    }]

    return data


def fetch_redemption_status(fund_code: str, start_date: str = None, end_date: str = None) -> List[Dict[str, Any]]:
    raw_data = _fetch_purchase_em_data(fund_code)
    if raw_data is None:
        return []

    fee_data = _fetch_redemption_fee_data(fund_code)

    today = datetime.now().strftime("%Y-%m-%d")
    data = [{
        "代码": fund_code,
        "日期": today,
        "赎回状态": raw_data.get("赎回状态"),
        "赎回限额": fee_data.get("赎回限额") if fee_data else None,
        "赎回手续费率": fee_data.get("赎回手续费率") if fee_data else None,
        "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    }]

    return data


def get_fund_share_type(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 份额类型 FROM fund_basic_info WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result else None
    except Exception as e:
        logger.error(f"Error getting fund share type: {e}")
        return None


def get_latest_nav_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(日期) FROM fund_nav_history WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result and result[0] else None
    except Exception as e:
        logger.error(f"Error getting latest nav date: {e}")
        return None


def batch_get_latest_nav_dates(fund_codes: List[str], db_path: str = DATABASE_PATH) -> Dict[str, Optional[str]]:
    if not fund_codes:
        return {}
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            placeholders = ",".join(["?"] * len(fund_codes))
            cursor.execute(f"""
                SELECT 代码, MAX(日期) as latest_date
                FROM fund_nav_history
                WHERE 代码 IN ({placeholders})
                GROUP BY 代码
            """, fund_codes)
            return {row[0]: row[1] for row in cursor.fetchall()}
    except Exception as e:
        logger.error(f"Error batch getting latest nav dates: {e}")
        return {}


def batch_get_fund_info(fund_codes: List[str], db_path: str = DATABASE_PATH) -> Dict[str, Dict[str, Any]]:
    if not fund_codes:
        return {}
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            placeholders = ",".join(["?"] * len(fund_codes))

            # 基金成立日期和份额
            cursor.execute(f"""
                SELECT 代码, 成立日期, 份额类型
                FROM fund_basic_info
                WHERE 代码 IN ({placeholders})
            """, fund_codes)
            establishment_dates = {}
            share_types = {}
            for row in cursor.fetchall():
                code, est_date, share_type = row[0], row[1], row[2]
                share_types[code] = share_type
                parsed_date = None
                if est_date:
                    parsed_date = parse_chinese_date(est_date)
                establishment_dates[code] = parsed_date

            # 最新净值日期
            cursor.execute(f"""
                SELECT 代码, MAX(日期) as latest_date
                FROM fund_nav_history
                WHERE 代码 IN ({placeholders})
                GROUP BY 代码
            """, fund_codes)
            latest_nav_dates = {row[0]: row[1] for row in cursor.fetchall()}

            # 基金绩效记录
            cursor.execute(f"""
                SELECT 代码, 周期类型, 周期值
                FROM fund_performance
                WHERE 代码 IN ({placeholders})
            """, fund_codes)
            perf_records = cursor.fetchall()
            perf_by_fund = {}
            for code, period_type, period_value in perf_records:
                if code not in perf_by_fund:
                    perf_by_fund[code] = []
                perf_by_fund[code].append((period_type, period_value))

            result = {}
            current_year = datetime.now().year
            for code in fund_codes:
                result[code] = {
                    "establishment_date": establishment_dates.get(code),
                    "latest_nav_date": latest_nav_dates.get(code),
                    "performance_records": perf_by_fund.get(code, []),
                    "current_year": current_year,
                    "share_type": share_types.get(code)
                }
            return result
    except Exception as e:
        logger.error(f"Error batch getting fund info: {e}")
        return {code: {"establishment_date": None, "latest_nav_date": None, "performance_records": [], "current_year": datetime.now().year, "share_type": None} for code in fund_codes}


def get_latest_manager_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(任职开始日期) FROM fund_manager WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result and result[0] else None
    except Exception as e:
        logger.error(f"Error getting latest manager date: {e}")
        return None


def get_latest_corporate_action_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(除权日期) FROM fund_corporate_actions WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result and result[0] else None
    except Exception as e:
        logger.error(f"Error getting latest corporate action date: {e}")
        return None


def has_performance_data(fund_code: str, db_path: str = DATABASE_PATH) -> bool:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT COUNT(*) FROM fund_performance WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result and result[0] > 0
    except Exception as e:
        logger.error(f"Error checking performance data: {e}")
        return False


def check_performance_complete(
    fund_code: str = None,
    perf_records: List[Tuple[str, Any]] = None,
    fund_start_date: Optional[datetime] = None,
    db_path: str = DATABASE_PATH
) -> Tuple[bool, bool]:
    if perf_records is None and fund_code:
        try:
            with get_db_connection(db_path) as conn:
                cursor = conn.cursor()
                cursor.execute("""
                    SELECT 周期类型, 周期值
                    FROM fund_performance
                    WHERE 代码 = ?
                """, (fund_code,))
                perf_records = cursor.fetchall()
        except Exception as e:
            logger.error(f"Error getting performance records: {e}")
            perf_records = []
    
    if not fund_start_date and fund_code:
        fund_start_date = get_fund_establishment_date(fund_code, db_path)
    
    if not fund_start_date:
        return False, False
    
    current_year = datetime.now().year
    fund_start_year = fund_start_date.year
    
    has_data = len(perf_records) > 0
    base_complete = len(perf_records) >= 8
    
    if fund_start_year == current_year:
        historical_complete = True
    else:
        expected_historical_years = current_year - fund_start_year
        historical_years_count = sum(
            1 for p in perf_records
            if p[0] == "历史年份" and p[1] and int(p[1]) >= fund_start_year
        )
        historical_complete = historical_years_count >= expected_historical_years
    
    return has_data, base_complete and historical_complete


def are_performance_records_complete(
    perf_records: List[Tuple[str, Any]],
    fund_start_date: Optional[datetime],
    current_year: int
) -> bool:
    _, is_complete = check_performance_complete(
        perf_records=perf_records,
        fund_start_date=fund_start_date
    )
    return is_complete


def is_performance_complete(fund_code: str, db_path: str = DATABASE_PATH, fund_start_date: datetime = None) -> Tuple[bool, bool]:
    return check_performance_complete(
        fund_code=fund_code,
        fund_start_date=fund_start_date,
        db_path=db_path
    )


def get_latest_performance_update_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(更新日期) FROM fund_performance WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result and result[0] else None
    except Exception as e:
        logger.error(f"Error getting latest performance update date: {e}")
        return None


def needs_performance_recalc(fund_code: str, db_path: str = DATABASE_PATH) -> bool:
    latest_nav_date = get_latest_nav_date(fund_code, db_path)
    if not latest_nav_date:
        return False
    
    latest_perf_update = get_latest_performance_update_date(fund_code, db_path)
    if not latest_perf_update:
        return True
    
    try:
        nav_date = datetime.strptime(latest_nav_date, "%Y-%m-%d")
        perf_update_date = datetime.strptime(latest_perf_update[:10], "%Y-%m-%d")
        return nav_date > perf_update_date
    except (ValueError, TypeError):
        return True


def get_latest_asset_scale_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(日期) FROM fund_asset_scale WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result and result[0] else None
    except Exception as e:
        logger.error(f"Error getting latest asset scale date: {e}")
        return None


def get_latest_purchase_status_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(日期) FROM fund_purchase_status WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result and result[0] else None
    except Exception as e:
        logger.error(f"Error getting latest purchase status date: {e}")
        return None


def get_latest_redemption_status_date(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[str]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT MAX(日期) FROM fund_redemption_status WHERE 代码 = ?", (fund_code,))
            result = cursor.fetchone()
            return result[0] if result and result[0] else None
    except Exception as e:
        logger.error(f"Error getting latest redemption status date: {e}")
        return None


def get_update_start_date(latest_date: str, start_date: str, end_date: str) -> Optional[str]:
    if latest_date:
        return (datetime.strptime(latest_date, "%Y-%m-%d") + timedelta(days=1)).strftime("%Y-%m-%d")
    if start_date:
        return start_date
    return None


def validate_date_format(date_str: str) -> bool:
    try:
        datetime.strptime(date_str, "%Y-%m-%d")
        return True
    except (ValueError, TypeError):
        return False


@functools.lru_cache(maxsize=10000)
def _cached_parse_date(date_str: str) -> Optional[datetime]:
    try:
        return datetime.strptime(date_str, "%Y-%m-%d")
    except (ValueError, TypeError):
        return None


def parse_date(date_str: Optional[str]) -> Optional[datetime]:
    if not date_str:
        return None
    return _cached_parse_date(str(date_str))


def validate_nav_data(nav_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not nav_data:
        return False, "净值数据为空"
    fund_code = nav_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    if not re.match(r"^\d{6}$", fund_code):
        return False, f"基金{fund_code}代码格式错误"
    date_str = nav_data.get("日期")
    if not date_str or not validate_date_format(date_str):
        return False, f"基金{fund_code}日期格式错误: {date_str}"
    unit_nav = nav_data.get("单位净值")
    if unit_nav is None or not isinstance(unit_nav, (int, float)):
        return False, f"基金{fund_code}单位净值无效"
    if unit_nav <= 0:
        return False, f"基金{fund_code}单位净值必须大于0: {unit_nav}"
    # cumulative_nav = nav_data.get("累计净值")
    # if cumulative_nav is not None and isinstance(cumulative_nav, (int, float)):
    #     if cumulative_nav < unit_nav:
    #         return False, f"基金{fund_code}累计净值({cumulative_nav})不能小于单位净值({unit_nav})"
    daily_growth = nav_data.get("日增长率")
    if daily_growth is not None and isinstance(daily_growth, (int, float)):
        if abs(daily_growth) > 100:
            logger.warning(f"基金{fund_code}日增长率异常: {daily_growth}%")
    return True, ""


def validate_basic_info(basic_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not basic_data:
        return False, "基本数据为空"
    fund_code = basic_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    if not re.match(r"^\d{6}$", fund_code):
        return False, f"基金{fund_code}代码格式错误"
    fund_name = basic_data.get("名称")
    if not fund_name or not isinstance(fund_name, str):
        return False, f"基金{fund_code}名称无效"
    if len(fund_name) > 100:
        return False, f"基金{fund_code}名称过长: {len(fund_name)}"
    manager = basic_data.get("基金管理人")
    if manager and len(manager) > 100:
        return False, f"基金{fund_code}基金管理人名称过长: {len(manager)}"
    return True, ""


def validate_performance_data(perf_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not perf_data:
        return False, "业绩数据为空"
    fund_code = perf_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    period_type = perf_data.get("周期类型")
    if not period_type or not isinstance(period_type, str):
        return False, f"基金{fund_code}周期类型无效"
    valid_period_types = ALL_PERIOD_TYPES
    if period_type not in valid_period_types:
        return False, f"基金{fund_code}周期类型无效: {period_type}"
    annual_return = perf_data.get("年化收益率")
    if annual_return is not None and isinstance(annual_return, (int, float)):
        if abs(annual_return) > 2000:
            logger.warning(f"基金{fund_code}年化收益率异常: {annual_return}%")
    max_drawdown = perf_data.get("最大回撤")
    if max_drawdown is not None and isinstance(max_drawdown, (int, float)):
        if max_drawdown > 0:
            logger.warning(f"基金{fund_code}最大回撤应为负数: {max_drawdown}")
    return True, ""


def validate_corporate_action(action_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not action_data:
        return False, "除权数据为空"
    fund_code = action_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    date_str = action_data.get("除权日期")
    if not date_str or not validate_date_format(date_str):
        return False, f"基金{fund_code}除权日期格式错误: {date_str}"
    # event_type = action_data.get("事件类型")
    # valid_types = ["分红", "拆分", "送股", "转增", "派息"]
    # if event_type and event_type not in valid_types:
    #     logger.warning(f"基金{fund_code}事件类型不在常见列表中: {event_type}")
    return True, ""


def validate_asset_scale(scale_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not scale_data:
        return False, "资产规模数据为空"
    fund_code = scale_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    date_str = scale_data.get("日期")
    if not date_str or not validate_date_format(date_str):
        return False, f"基金{fund_code}日期格式错误: {date_str}"
    asset_scale = scale_data.get("资产规模")
    if asset_scale is not None and isinstance(asset_scale, (int, float)):
        if asset_scale < 0:
            return False, f"基金{fund_code}资产规模不能为负: {asset_scale}"
    share_scale = scale_data.get("份额规模")
    if share_scale is not None and isinstance(share_scale, (int, float)):
        if share_scale < 0:
            return False, f"基金{fund_code}份额规模不能为负: {share_scale}"
    return True, ""


def validate_manager_data(manager_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not manager_data:
        return False, "基金经理数据为空"
    fund_code = manager_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    manager_name = manager_data.get("基金经理姓名")
    if not manager_name or not isinstance(manager_name, str):
        return False, f"基金{fund_code}基金经理姓名无效"
    if len(manager_name) > 50:
        return False, f"基金{fund_code}基金经理姓名过长: {len(manager_name)}"
    return True, ""


def validate_purchase_status(purchase_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not purchase_data:
        return False, "申购状态数据为空"
    fund_code = purchase_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    status = purchase_data.get("申购状态")
    if status and status not in ["开放申购", "暂停申购", "封闭申购", "限大额"]:
        logger.warning(f"基金{fund_code}申购状态异常: {status}")
    return True, ""


def validate_redemption_status(redemption_data: Dict[str, Any]) -> Tuple[bool, str]:
    if not redemption_data:
        return False, "赎回状态数据为空"
    fund_code = redemption_data.get("代码")
    if not fund_code or not isinstance(fund_code, str):
        return False, "基金代码无效"
    status = redemption_data.get("赎回状态")
    if status and status not in ["开放赎回", "暂停赎回", "封闭赎回", "限大额"]:
        logger.warning(f"基金{fund_code}赎回状态异常: {status}")
    fee_rate = redemption_data.get("赎回手续费率")
    if fee_rate is not None and isinstance(fee_rate, (int, float)):
        if fee_rate < 0 or fee_rate > 1:
            return False, f"基金{fund_code}赎回手续费率超出合理范围: {fee_rate}"
    return True, ""


def has_new_corporate_actions(fund_code: str, db_path: str = DATABASE_PATH) -> bool:
    try:
        latest_action_date = get_latest_corporate_action_date(fund_code, db_path)
        if not latest_action_date:
            return True
        corporate_actions = fetch_corporate_actions(fund_code, latest_action_date, datetime.now().strftime("%Y-%m-%d"))
        return len(corporate_actions) > 0
    except Exception as e:
        logger.error(f"Error checking new corporate actions: {e}")
        return False


def get_basic_info_from_db(fund_code: str, db_path: str = DATABASE_PATH) -> Optional[Dict[str, Any]]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT 代码, 名称, 基金类型, 份额类型, 主基金代码, 成立日期, 上市日期,
                       基金管理人, 基金托管人, 管理费率, 托管费率, 销售服务费率,
                       业绩比较基准, 跟踪标的, 投资风格, 风险等级, 更新日期
                FROM fund_basic_info WHERE 代码 = ?
            """, (fund_code,))
            row = cursor.fetchone()
            if row:
                return {
                    "代码": row[0], "名称": row[1], "基金类型": row[2], "份额类型": row[3],
                    "主基金代码": row[4], "成立日期": row[5], "上市日期": row[6],
                    "基金管理人": row[7], "基金托管人": row[8], "管理费率": row[9],
                    "托管费率": row[10], "销售服务费率": row[11], "业绩比较基准": row[12],
                    "跟踪标的": row[13], "投资风格": row[14], "风险等级": row[15], "更新日期": row[16]
                }
            return None
    except Exception as e:
        logger.error(f"Error getting basic info from db: {e}")
        return None


def save_basic_info(fund_data: Dict[str, Any], db_path: str = DATABASE_PATH) -> bool:
    fund_code = fund_data.get("代码")
    if not fund_code:
        return False

    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            try:
                cursor.execute("""
                    INSERT INTO fund_basic_info (
                        代码, 名称, 基金类型, 基金子类型, 份额类型, 主基金代码, 成立日期, 上市日期,
                        基金管理人, 基金托管人, 管理费率, 托管费率, 销售服务费率,
                        业绩比较基准, 跟踪标的, 投资风格, 风险等级, 更新日期
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    fund_code, fund_data.get("名称"), fund_data.get("基金类型"),
                    fund_data.get("基金子类型"), fund_data.get("份额类型"), fund_data.get("主基金代码"),
                    fund_data.get("成立日期"), fund_data.get("上市日期"), fund_data.get("基金管理人"),
                    fund_data.get("基金托管人"), fund_data.get("管理费率"), fund_data.get("托管费率"),
                    fund_data.get("销售服务费率"), fund_data.get("业绩比较基准"), fund_data.get("跟踪标的"),
                    fund_data.get("投资风格"), fund_data.get("风险等级"), fund_data.get("更新日期")
                ))
                conn.commit()
                return True
            except Exception as insert_error:
                if "UNIQUE constraint failed" in str(insert_error) or "Duplicate entry" in str(insert_error):
                    return False
                raise insert_error

        fund_code_value = fund_data.get("代码")
        share_type = fund_data.get("份额类型", "")
        if share_type == "A":
            fund_name = fund_data.get("名称", "")
            base_name = fund_name[:-1] if fund_name else fund_name
            if base_name:
                cursor.execute("""
                    UPDATE fund_basic_info
                    SET 主基金代码 = ?, 更新日期 = ?
                    WHERE 名称 LIKE ? AND 主基金代码 IS NULL AND 代码 != ?
                """, (fund_code_value, fund_data.get("更新日期"), base_name + "%", fund_code_value))
                conn.commit()

    except Exception as e:
        logger.error(f"Error saving basic info: {e}")
        return False


def save_basic_info_batch(fund_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not fund_data_list:
        return 0

    try:
        with get_db_connection(db_path, optimize=True) as conn:
            cursor = conn.cursor()

            params = [
                (
                    data.get("代码"), data.get("名称"), data.get("基金类型"),
                    data.get("基金子类型"), data.get("份额类型"), data.get("主基金代码"),
                    data.get("成立日期"), data.get("上市日期"), data.get("基金管理人"),
                    data.get("基金托管人"), data.get("管理费率"), data.get("托管费率"),
                    data.get("销售服务费率"), data.get("业绩比较基准"), data.get("跟踪标的"),
                    data.get("投资风格"), data.get("风险等级"), data.get("更新日期")
                )
                for data in fund_data_list
            ]

            cursor.executemany("""
                INSERT OR IGNORE INTO fund_basic_info (
                    代码, 名称, 基金类型, 基金子类型, 份额类型, 主基金代码, 成立日期, 上市日期,
                    基金管理人, 基金托管人, 管理费率, 托管费率, 销售服务费率,
                    业绩比较基准, 跟踪标的, 投资风格, 风险等级, 更新日期
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, params)

            conn.commit()
            return cursor.rowcount

    except Exception as e:
        logger.error(f"Error saving basic info batch: {e}")
        return 0


def save_nav_history(nav_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not nav_data_list:
        return 0

    try:
        with get_db_connection(db_path, optimize=True) as conn:
            cursor = conn.cursor()

            valid_data = []
            for nav_data in nav_data_list:
                is_valid, error_msg = validate_nav_data(nav_data)
                if not is_valid:
                    logger.warning(f"Skipping invalid nav data: {error_msg}")
                    continue
                valid_data.append(nav_data)

            if not valid_data:
                return 0

            params = [
                (
                    d.get("代码"), d.get("日期"),
                    d.get("单位净值"), d.get("累计净值"),
                    d.get("日增长率"), d.get("更新日期")
                )
                for d in valid_data
            ]

            cursor.executemany("""
                INSERT OR REPLACE INTO fund_nav_history (
                    代码, 日期, 单位净值, 累计净值, 日增长率, 更新日期
                ) VALUES (?, ?, ?, ?, ?, ?)
            """, params)

            conn.commit()
            return cursor.rowcount
    except Exception as e:
        logger.error(f"Error saving nav history: {e}")
        return 0


def save_performance(performance_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not performance_data_list:
        return 0

    try:
        with get_db_connection(db_path, optimize=True) as conn:
            cursor = conn.cursor()

            valid_data = []
            for perf_data in performance_data_list:
                is_valid, error_msg = validate_performance_data(perf_data)
                if not is_valid:
                    logger.warning(f"Skipping invalid performance data: {error_msg}")
                    continue
                valid_data.append(perf_data)

            if not valid_data:
                return 0

            params = [
                (
                    d.get("代码"), d.get("周期类型"), d.get("周期值"),
                    d.get("净值增长率"), d.get("最大回撤"), d.get("下行标准差"),
                    d.get("夏普比率"), d.get("索提诺比率"), d.get("卡玛比率"),
                    d.get("年化收益率"), d.get("波动率"),
                    d.get("同类型基金排名"), d.get("同类型基金总数"),
                    d.get("更新日期")
                )
                for d in valid_data
            ]

            cursor.executemany("""
                INSERT OR REPLACE INTO fund_performance (
                    代码, 周期类型, 周期值, 净值增长率, 最大回撤, 下行标准差,
                    夏普比率, 索提诺比率, 卡玛比率, 年化收益率, 波动率,
                    同类型基金排名, 同类型基金总数, 更新日期
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, params)

            conn.commit()
            return cursor.rowcount
    except Exception as e:
        logger.error(f"Error saving performance: {e}")
        return 0


def save_corporate_actions(actions_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not actions_data_list:
        return 0

    count = 0
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 代码 || '-' || 除权日期 || '-' || 事件类型 FROM fund_corporate_actions")
            existing = set(cursor.fetchall())

            for action_data in actions_data_list:
                fund_code = action_data.get("代码")
                action_date = action_data.get("除权日期")
                action_type = action_data.get("事件类型")
                if not fund_code or not action_date:
                    continue

                key = f"{fund_code}-{action_date}-{action_type}"
                if key in existing:
                    continue

                is_valid, error_msg = validate_corporate_action(action_data)
                if not is_valid:
                    logger.warning(f"Skipping invalid corporate action: {error_msg}")
                    continue

                cursor.execute("""
                    INSERT INTO fund_corporate_actions (
                        代码, 除权日期, 事件类型, 每份分红金额, 分红发放日, 拆分比例,
                        权益登记日, 事件描述, 公告日期, 更新日期
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    fund_code, action_date, action_type,
                    action_data.get("每份分红金额"), action_data.get("分红发放日"),
                    action_data.get("拆分比例"), action_data.get("权益登记日"),
                    action_data.get("事件描述"), action_data.get("公告日期"),
                    action_data.get("更新日期")
                ))
                count += 1
                existing.add(key)
            conn.commit()
            return count
    except Exception as e:
        logger.error(f"Error saving corporate actions: {e}")
        return count


def get_corporate_actions_count(fund_code: str, db_path: str = DATABASE_PATH) -> int:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("""
                SELECT COUNT(*) FROM fund_corporate_actions WHERE 代码 = ?
            """, (fund_code,))
            result = cursor.fetchone()
            return result[0] if result else 0
    except Exception as e:
        logger.error(f"Error getting corporate actions count: {e}")
        return 0


def save_managers(managers_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not managers_data_list:
        return 0

    count = 0
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            for manager_data in managers_data_list:
                fund_code = manager_data.get("代码")
                manager_name = manager_data.get("基金经理姓名")
                start_date = manager_data.get("任职开始日期")
                if not fund_code or not manager_name:
                    continue

                is_valid, error_msg = validate_manager_data(manager_data)
                if not is_valid:
                    logger.warning(f"Skipping invalid manager data: {error_msg}")
                    continue

                cursor.execute("""
                    INSERT OR IGNORE INTO fund_manager (
                        代码, 基金经理姓名, 任职开始日期, 任职结束日期, 管理天数, 更新日期
                    ) VALUES (?, ?, ?, ?, ?, ?)
                """, (
                    fund_code, manager_name, start_date,
                    manager_data.get("任职结束日期"),
                    manager_data.get("管理天数"), manager_data.get("更新日期")
                ))
                count += 1
            conn.commit()
            return count
    except Exception as e:
        logger.error(f"Error saving managers: {e}")
        return count


def save_asset_scale(scale_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not scale_data_list:
        return 0

    count = 0
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 代码 || '-' || 日期 FROM fund_asset_scale")
            existing = set(cursor.fetchall())

            for scale_data in scale_data_list:
                fund_code = scale_data.get("代码")
                date = scale_data.get("日期")
                if not fund_code or not date:
                    continue

                key = f"{fund_code}-{date}"
                if key in existing:
                    continue

                is_valid, error_msg = validate_asset_scale(scale_data)
                if not is_valid:
                    logger.warning(f"Skipping invalid asset scale: {error_msg}")
                    continue

                cursor.execute("""
                    INSERT INTO fund_asset_scale (
                        代码, 日期, 资产规模, 份额规模, 更新日期
                    ) VALUES (?, ?, ?, ?, ?)
                """, (
                    fund_code, date,
                    scale_data.get("资产规模"), scale_data.get("份额规模"),
                    scale_data.get("更新日期")
                ))
                count += 1
                existing.add(key)
            conn.commit()
            return count
    except Exception as e:
        logger.error(f"Error saving asset scale: {e}")
        return count


def save_purchase_status(purchase_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not purchase_data_list:
        return 0

    count = 0
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            for purchase_data in purchase_data_list:
                fund_code = purchase_data.get("代码")
                date = purchase_data.get("日期")
                if not fund_code or not date:
                    continue

                is_valid, error_msg = validate_purchase_status(purchase_data)
                if not is_valid:
                    logger.warning(f"Skipping invalid purchase status: {error_msg}")
                    continue

                cursor.execute("""
                    INSERT OR IGNORE INTO fund_purchase_status (
                        代码, 日期, 申购状态, 申购限额, 申购手续费率, 更新日期
                    ) VALUES (?, ?, ?, ?, ?, ?)
                """, (
                    fund_code, date,
                    purchase_data.get("申购状态"), purchase_data.get("申购限额"),
                    purchase_data.get("申购手续费率"), purchase_data.get("更新日期")
                ))
                count += 1
            conn.commit()
            return count
    except Exception as e:
        logger.error(f"Error saving purchase status: {e}")
        return count


def save_redemption_status(redemption_data_list: List[Dict[str, Any]], db_path: str = DATABASE_PATH) -> int:
    if not redemption_data_list:
        return 0

    count = 0
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            for redemption_data in redemption_data_list:
                fund_code = redemption_data.get("代码")
                date = redemption_data.get("日期")
                if not fund_code or not date:
                    continue

                is_valid, error_msg = validate_redemption_status(redemption_data)
                if not is_valid:
                    logger.warning(f"Skipping invalid redemption status: {error_msg}")
                    continue

                cursor.execute("""
                    INSERT OR IGNORE INTO fund_redemption_status (
                        代码, 日期, 赎回状态, 赎回限额, 赎回手续费率, 更新日期
                    ) VALUES (?, ?, ?, ?, ?, ?)
                """, (
                    fund_code, date,
                    redemption_data.get("赎回状态"), redemption_data.get("赎回限额"),
                    redemption_data.get("赎回手续费率"), redemption_data.get("更新日期")
                ))
                count += 1
            conn.commit()
            return count
    except Exception as e:
        logger.error(f"Error saving redemption status: {e}")
        return count


def record_update_history(
    update_type: str,
    table_name: str,
    status: str,
    record_count: int,
    new_record_count: int,
    elapsed_time: int,
    error_message: str = "",
    fund_codes: List[str] = None,
    source: str = "manual",
    operator: str = "system",
    db_path: str = DATABASE_PATH
) -> bool:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            fund_codes_str = json.dumps(fund_codes, ensure_ascii=False) if fund_codes else ""
            cursor.execute("""
                INSERT INTO fund_info_update_history (
                    基金代码列表, 更新类型, 更新表名, 更新状态, 更新记录数,
                    新增记录数, 更新耗时, 错误信息, 更新来源, 操作人, 更新日期
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                fund_codes_str, update_type, table_name, status,
                record_count, new_record_count, elapsed_time,
                error_message, source, operator,
                datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            ))
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Error recording update history: {e}")
        return False


def recalculate_performance_only(fund_code: str, db_path: str = DATABASE_PATH) -> Tuple[int, int]:
    if not fund_code or not isinstance(fund_code, str):
        logger.error(f"Invalid fund_code: {fund_code}")
        return 0, 0

    fund_code = fund_code.strip()

    if not os.path.exists(db_path):
        logger.error(f"Database does not exist: {db_path}")
        return 0, 0

    total_records = 0
    new_records = 0

    try:
        nav_data = get_nav_history_from_db(fund_code, db_path)
        if not nav_data or len(nav_data) < 2:
            logger.warning(f"No nav data for fund {fund_code}")
            return 0, 0

        existing_basic = get_basic_info_from_db(fund_code, db_path)
        fund_start_date = None
        if existing_basic and existing_basic.get("成立日期"):
            fund_start_date = parse_chinese_date(existing_basic["成立日期"])

        if not fund_start_date:
            fund_start_date = get_fund_establishment_date(fund_code, db_path)

        df = pd.DataFrame(nav_data)
        df["日期"] = pd.to_datetime(df["日期"])
        has_data = len(df) > 0

        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()
            cursor.execute("DELETE FROM fund_performance WHERE 代码 = ?", (fund_code,))
            conn.commit()
        logger.info(f"Deleted existing performance data for fund {fund_code}")

        performance = fetch_fund_performance(
            fund_code, db_path,
            is_first_insert=True,
            fund_start_date=fund_start_date,
            use_cache=False
        )

        if performance:
            saved_count = save_performance(performance, db_path)
            total_records += saved_count
            new_records += saved_count
            logger.info(f"Saved performance data for fund {fund_code}: {saved_count} records")

    except Exception as e:
        logger.error(f"Error in recalculate_performance_only for fund {fund_code}: {e}")

    return total_records, new_records


def update_single_fund(fund_code: str, start_date: str = None, end_date: str = None, db_path: str = DATABASE_PATH, use_cache: bool = True, skip_perf_save: bool = False, fund_info: Dict[str, Any] = None) -> Tuple[int, int, List[Dict[str, Any]]]:
    if not fund_code or not isinstance(fund_code, str):
        logger.error(f"Invalid fund_code: {fund_code}")
        return 0, 0, []

    fund_code = fund_code.strip()

    if not end_date:
        end_date = datetime.now().strftime("%Y-%m-%d")

    if not validate_date_format(end_date):
        logger.error(f"Invalid end_date format: {end_date}")
        return 0, 0, []

    if start_date and not validate_date_format(start_date):
        logger.error(f"Invalid start_date format: {start_date}")
        return 0, 0, []

    if not os.path.exists(db_path):
        logger.error(f"Database does not exist: {db_path}")
        return 0, 0, []

    total_records = 0
    new_records = 0
    performance_list = []

    try:
        share_type = None
        if fund_info and fund_info.get("share_type"):
            share_type = fund_info.get("share_type")

        existing_basic = None
        if share_type is None:
            existing_basic = get_basic_info_from_db(fund_code, db_path)
            if existing_basic:
                share_type = existing_basic.get("份额类型")

        if not share_type:
            try:
                existing_basic = fetch_fund_basic_info(fund_code, db_path, use_cache=use_cache)
                if existing_basic:
                    share_type = existing_basic.get("份额类型")
                    if save_basic_info(existing_basic, db_path):
                        total_records += 1
                        new_records += 1
                        logger.info(f"Saved basic info for fund {fund_code}")
            except Exception as e:
                logger.warning(f"Failed to fetch basic info for fund {fund_code}: {e}")

        fund_start_date = None
        if fund_info and fund_info.get("establishment_date"):
            fund_start_date = fund_info.get("establishment_date")
        else:
            if existing_basic and existing_basic.get("成立日期"):
                fund_start_date = parse_chinese_date(existing_basic["成立日期"])
            if not fund_start_date:
                fund_start_date = get_fund_establishment_date(fund_code, db_path)

        nav_updated = False

        table_update_handlers = [
            ("fund_nav_history", get_latest_nav_date, fetch_fund_nav_history, save_nav_history),
            # ("action", get_latest_corporate_action_date, fetch_corporate_actions, save_corporate_actions),
            # ("manager", get_latest_manager_date, fetch_fund_managers, save_managers),
            # ("asset", get_latest_asset_scale_date, fetch_asset_scale, save_asset_scale),
            # ("purchase", get_latest_purchase_status_date, fetch_purchase_status, save_purchase_status),
            # ("redemption", get_latest_redemption_status_date, fetch_redemption_status, save_redemption_status),
        ]

        fetch_kwargs = {}
        if fund_start_date:
            fetch_kwargs["fund_start_date"] = fund_start_date
        fetch_kwargs["db_path"] = db_path
        fetch_kwargs["use_cache"] = use_cache

        # 保存当前基金的净值历史数据
        nav_data = None
        for table_name, get_latest_fn, fetch_fn, save_fn in table_update_handlers:
            try:
                latest_date = get_latest_fn(fund_code, db_path)
                update_start = get_update_start_date(latest_date, start_date, end_date)

                if update_start and update_start >= end_date:
                    logger.debug(f"Skipping {table_name} update for fund {fund_code}: update_start ({update_start}) >= end_date ({end_date})")
                    continue

                data = retry_operation(lambda: fetch_fn(fund_code, update_start, end_date, **fetch_kwargs))
                if data:
                    saved_count = retry_operation(lambda: save_fn(data, db_path))
                    total_records += saved_count
                    new_records += saved_count
                    if table_name == "fund_nav_history":
                        nav_updated = True
                        nav_data = data
                    logger.info(f"Updated {table_name} for fund {fund_code}: {saved_count} records")
                else:
                    logger.debug(f"No {table_name} data returned for fund {fund_code}")
            except Exception as e:
                logger.error(f"Error updating {table_name} for fund {fund_code}: {e}")
                continue
        
        # 计算A类基金的性能数据
        if share_type != "A":
            return total_records, new_records, performance_list
        try:
            has_data, is_complete = is_performance_complete(fund_code, db_path, fund_start_date)
            is_first = not has_data
            nav_newer_than_perf = needs_performance_recalc(fund_code, db_path)
            needs_recalc = nav_updated or not is_complete or nav_newer_than_perf
            if is_first or needs_recalc:
                performance = fetch_fund_performance(
                    fund_code, db_path,
                    is_first_insert=is_first,
                    fund_start_date=fund_start_date,
                    use_cache=use_cache,
                    nav_data=nav_data
                )
                if performance:
                    if skip_perf_save:
                        performance_list.extend(performance)
                    else:
                        saved_count = save_performance(performance, db_path)
                        total_records += saved_count
                        new_records += saved_count
                        logger.info(f"Saved performance data for fund {fund_code}: {saved_count} records")
        except Exception as e:
            logger.error(f"Error calculating performance for fund {fund_code}: {e}")

    except Exception as e:
        logger.error(f"Error in update_single_fund for fund {fund_code}: {e}")
        return total_records, new_records, []

    

class FundImporter:
    def __init__(self, db_path: str, max_workers: int = None):
        self.db_path = db_path
        self.max_workers = max_workers or DEFAULT_MAX_WORKERS

    def _prepare_fund_lists(self, all_fund_codes: List[str]) -> Tuple[List[str], List[str], List[str]]:
        existing_codes = set(get_all_fund_codes_fromDb(self.db_path))

        new_fund_codes = [code for code in all_fund_codes if code not in existing_codes]
        funds_to_update = [code for code in all_fund_codes if code in existing_codes]

        yesterday = datetime.now() - timedelta(days=1)

        funds_need_update = []
        funds_skip = []

        fund_info_batch = batch_get_fund_info(funds_to_update, self.db_path)

        if not fund_info_batch and funds_to_update:
            logger.warning("batch_get_fund_info returned empty, treating all funds as needing update")
            funds_need_update = funds_to_update
        else:
            for code in funds_to_update:
                info = fund_info_batch.get(code, {})
                latest_nav_date = info.get("latest_nav_date")
                perf_records = info.get("performance_records", [])
                fund_start_date = info.get("establishment_date")

                latest_nav_date = parse_date(latest_nav_date) if latest_nav_date else None
                needs_nav = latest_nav_date is None or latest_nav_date < yesterday
                needs_perf = not are_performance_records_complete(perf_records, fund_start_date, info.get("current_year"))
                if needs_nav or needs_perf:
                    funds_need_update.append(code)
                else:
                    funds_skip.append(code)

        if funds_skip:
            logger.info(f"Skipping {len(funds_skip)} funds with up-to-date data")

        if new_fund_codes:
            logger.info(f"Found {len(new_fund_codes)} new funds to add")

        logger.info(f"Updating {len(funds_need_update)} existing funds + {len(new_fund_codes)} new funds")

        return new_fund_codes, funds_need_update, funds_skip, fund_info_batch

    def _execute_updates(
        self,
        new_fund_codes: List[str],
        update_fund_codes: List[str],
        use_cache: bool,
        fund_info_batch: Dict[str, Dict[str, Any]] = None
    ) -> Tuple[int, int, List[str]]:
        total_records = 0
        new_records = 0
        failed_funds = []

        if new_fund_codes:
            new_records_count, new_new_count, new_failed = self._process_funds(
                new_fund_codes, use_cache, None, is_new=True
            )
            total_records += new_records_count
            new_records += new_new_count
            failed_funds.extend(new_failed)

        if update_fund_codes:
            update_records_count, update_new_count, update_failed = self._process_funds(
                update_fund_codes, use_cache, fund_info_batch, is_new=False
            )
            total_records += update_records_count
            new_records += update_new_count
            failed_funds.extend(update_failed)

        return total_records, new_records, failed_funds

    def _process_funds(
        self,
        fund_codes: List[str],
        use_cache: bool,
        fund_info_batch: Dict[str, Dict[str, Any]] = None,
        batch_size: int = 100,
        is_new: bool = False
    ) -> Tuple[int, int, List[str]]:
        if not fund_codes:
            return 0, 0, []

        total_records = 0
        new_records = 0
        failed_funds = []
        total_funds = len(fund_codes)
        prefix = "[New]" if is_new else "[Update]"

        logger.info(f"Processing {total_funds} {'new' if is_new else 'update'} funds with batch_size={batch_size}")

        for batch_start in range(0, total_funds, batch_size):
            batch_end = min(batch_start + batch_size, total_funds)
            batch_codes = fund_codes[batch_start:batch_end]
            batch_num = batch_start // batch_size + 1
            total_batches = (total_funds + batch_size - 1) // batch_size

            logger.info(f"[{'New Funds' if is_new else 'Update Funds'}] Batch {batch_num}/{total_batches} ({len(batch_codes)} funds)")

            max_workers = min(self.max_workers * 2, MAX_CONCURRENT_WORKERS)
            all_performance = []
            with ThreadPoolExecutor(max_workers=max_workers) as executor:
                future_to_code = {
                    executor.submit(
                        update_single_fund, code, None, None, self.db_path, use_cache, True,
                        fund_info_batch.get(code) if fund_info_batch else None
                    ): code
                    for code in batch_codes
                }

                for future in as_completed(future_to_code):
                    fund_code = future_to_code[future]
                    try:
                        result = future.result()
                        if result is None:
                            logger.warning(f"{prefix} Fund {fund_code} returned None")
                            failed_funds.append(fund_code)
                            continue
                        records, new_count, perf_list = result
                        total_records += records
                        new_records += new_count
                        if perf_list:
                            all_performance.extend(perf_list)
                        logger.info(f"{prefix} Updated fund {fund_code}: {records} records, {new_count} new")
                    except Exception as e:
                        logger.error(f"{prefix} Error updating fund {fund_code}: {e}")
                        failed_funds.append(fund_code)

            if all_performance:
                perf_count = save_performance(all_performance, self.db_path)
                total_records += perf_count
                new_records += perf_count
                logger.info(f"{prefix} Batch saved {perf_count} performance records")

            if batch_end < total_funds:
                gc.collect()

        return total_records, new_records, failed_funds

    def _finalize_update(
        self,
        total_records: int,
        new_records: int,
        failed_funds: List[str],
        fund_codes: List[str],
        start_time: float
    ) -> Dict[str, Any]:
        elapsed_time = int(time.time() - start_time)

        record_update_history(
            update_type="FULL",
            table_name="ALL",
            status="SUCCESS" if not failed_funds else "PARTIAL",
            record_count=total_records,
            new_record_count=new_records,
            elapsed_time=elapsed_time,
            error_message=f"Failed funds: {failed_funds}" if failed_funds else "",
            fund_codes=fund_codes,
            db_path=self.db_path
        )

        logger.info(f"Full update completed: {total_records} records, {new_records} new, {elapsed_time}s")

        return {
            "success": True,
            "total_records": total_records,
            "new_records": new_records,
            "elapsed_time": elapsed_time,
            "failed_funds": failed_funds
        }

    def import_fund_data(
        self,
        limit: int = None,
        only_a_share: bool = False,
        use_cache: bool = True
    ) -> Dict[str, Any]:
        logger.info("Starting full update")
        start_time = time.time()

        if not acquire_update_lock():
            return {"success": False, "error": "Failed to acquire update lock"}

        try:
            if not os.path.exists(self.db_path):
                create_database(self.db_path)

            all_fund_codes = fetch_fund_list_from_akshare(only_a_share=only_a_share, use_cache=use_cache)
            if limit:
                all_fund_codes = sorted(all_fund_codes)[:limit]

            new_fund_codes, funds_need_update, funds_skip, fund_info_batch = self._prepare_fund_lists(all_fund_codes)

            total_records, new_records, failed_funds = self._execute_updates(
                new_fund_codes, funds_need_update, use_cache, fund_info_batch
            )

            all_processed = new_fund_codes + funds_need_update
            return self._finalize_update(total_records, new_records, failed_funds, all_processed, start_time)

        except Exception as e:
            logger.error(f"Error in full update: {e}")
            elapsed_time = int(time.time() - start_time)
            record_update_history(
                update_type="FULL",
                table_name="ALL",
                status="FAILED",
                record_count=0,
                new_record_count=0,
                elapsed_time=elapsed_time,
                error_message=str(e),
                db_path=self.db_path
            )
            return {"success": False, "error": str(e)}
        finally:
            release_update_lock()


def import_fund_data(
    max_workers: int = None,
    db_path: str = DATABASE_PATH,
    limit: int = None,
    only_a_share: bool = True,
    use_cache: bool = True
) -> Dict[str, Any]:
    return FundImporter(db_path, max_workers).import_fund_data(
        limit=limit,
        only_a_share=only_a_share,
        use_cache=use_cache
    )


def batch_fetch_nav_history(
    fund_codes: List[str],
    db_path: str = DATABASE_PATH,
    max_workers: int = None,
    use_cache: bool = True
) -> Dict[str, List[Dict[str, Any]]]:
    """
    批量从akshare获取多个基金的净值历史（高并发）

    Args:
        fund_codes: 基金代码列表
        db_path: 数据库路径
        max_workers: 最大并发数
        use_cache: 是否使用缓存

    Returns:
        Dict[str, List[Dict]], key为基金代码，value为净值历史列表
    """
    max_workers = max_workers or MAX_CONCURRENT_WORKERS
    fund_nav_data: Dict[str, List[Dict[str, Any]]] = {}

    def fetch_for_fund(code: str) -> Tuple[str, List[Dict[str, Any]]]:
        try:
            nav_data = fetch_fund_nav_history(code, None, None, None, use_cache)
            return code, nav_data
        except Exception as e:
            logger.error(f"Error fetching nav for fund {code}: {e}")
            return code, []

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {
            executor.submit(fetch_for_fund, code): code
            for code in fund_codes
        }

        for future in as_completed(futures):
            code, nav_data = future.result()
            fund_nav_data[code] = nav_data

    return fund_nav_data


def batch_calculate_performance(
    fund_nav_data: Dict[str, List[Dict[str, Any]]],
    db_path: str = DATABASE_PATH,
    max_workers: int = None
) -> List[Dict[str, Any]]:
    """
    批量计算多个基金的绩效数据

    Args:
        fund_nav_data: Dict[基金代码, 净值历史列表]
        db_path: 数据库路径

    Returns:
        所有基金的绩效数据列表
    """
    max_workers = max_workers or MAX_CONCURRENT_WORKERS
    all_performance: List[Dict[str, Any]] = []

    def calc_for_fund(code: str, nav_data: List[Dict[str, Any]]) -> Tuple[str, List[Dict[str, Any]]]:
        try:
            performance = fetch_fund_performance(code, db_path, use_cache=False)
            return code, performance if performance else []
        except Exception as e:
            logger.error(f"Error calculating performance for fund {code}: {e}")
            return code, []

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {
            executor.submit(calc_for_fund, code, nav_data): code
            for code, nav_data in fund_nav_data.items()
        }

        for future in as_completed(futures):
            code, performance = future.result()
            all_performance.extend(performance)

    return all_performance


def import_fund_data_batch(
    max_workers: int = None,
    db_path: str = DATABASE_PATH,
    limit: int = None,
    only_a_share: bool = True,
    use_cache: bool = True
) -> Dict[str, Any]:
    """
    分阶段批量导入基金数据（优化版）

    阶段1: 批量获取和保存净值历史（高并发）
    阶段2: 批量计算和保存绩效数据

    Args:
        max_workers: 最大并发数
        db_path: 数据库路径
        limit: 限制导入基金数量
        only_a_share: 只导入A股基金
        use_cache: 使用缓存

    Returns:
        导入结果统计
    """
    max_workers = max_workers or MAX_CONCURRENT_WORKERS
    logger.info("Starting batch import (optimized)")
    start_time = time.time()

    if not acquire_update_lock():
        return {"success": False, "error": "Failed to acquire update lock"}

    try:
        if not os.path.exists(db_path):
            create_database(db_path)

        all_fund_codes = fetch_fund_list_from_akshare(only_a_share=only_a_share, use_cache=use_cache)
        if limit:
            all_fund_codes = all_fund_codes[:limit]

        logger.info(f"Total funds to process: {len(all_fund_codes)}")

        existing_codes = set(get_all_fund_codes_fromDb(db_path))
        new_fund_codes = [code for code in all_fund_codes if code not in existing_codes]
        update_fund_codes = [code for code in all_fund_codes if code in existing_codes]

        logger.info(f"New funds: {len(new_fund_codes)}, Update funds: {len(update_fund_codes)}")

        total_records = 0
        new_records = 0

        if new_fund_codes:
            logger.info(f"Phase 1: Batch fetching basic info for {len(new_fund_codes)} new funds")
            with ThreadPoolExecutor(max_workers=max_workers) as executor:
                futures = {
                    # 基金基本信息优先从缓存获取
                    executor.submit(fetch_fund_basic_info, code, db_path, use_cache=True): code
                    for code in new_fund_codes
                }
                basic_infoList = []
                for future in as_completed(futures):
                    code = futures[future]
                    try:
                        info = future.result()
                        if info:
                            info["代码"] = code
                            basic_infoList.append(info)
                    except Exception as e:
                        logger.error(f"Error fetching basic info for {code}: {e}")

                if basic_infoList:
                    saved = save_basic_info_batch(basic_infoList, db_path)
                    total_records += saved
                    new_records += saved
                    logger.info(f"Saved basic info for {saved} new funds")

            logger.info(f"Phase 2: Batch fetching nav history for {len(new_fund_codes)} new funds")
            new_fund_nav_data = batch_fetch_nav_history(new_fund_codes, db_path, max_workers, use_cache)

            nav_records = 0
            for code, nav_list in new_fund_nav_data.items():
                if nav_list:
                    saved = save_nav_history(nav_list, db_path)
                    nav_records += saved
            total_records += nav_records
            new_records += nav_records
            logger.info(f"Saved {nav_records} nav history records for new funds")

        if update_fund_codes:
            yesterday = datetime.now() - timedelta(days=1)
            fund_info_batch = batch_get_fund_info(update_fund_codes, db_path)

            funds_need_nav_update = []
            for code in update_fund_codes:
                info = fund_info_batch.get(code, {})
                latest_nav_date = info.get("latest_nav_date")
                if latest_nav_date:
                    latest_nav_dt = parse_date(latest_nav_date)
                    if latest_nav_dt and latest_nav_dt >= yesterday:
                        continue
                funds_need_nav_update.append(code)

            if funds_need_nav_update:
                logger.info(f"Phase 3: Batch updating nav history for {len(funds_need_nav_update)} funds")
                update_fund_nav_data = batch_fetch_nav_history(funds_need_nav_update, db_path, max_workers, use_cache)

                nav_records = 0
                for code, nav_list in update_fund_nav_data.items():
                    if nav_list:
                        saved = save_nav_history(nav_list, db_path)
                        nav_records += saved
                total_records += nav_records
                new_records += nav_records
                logger.info(f"Updated {nav_records} nav history records")

        logger.info("Phase 4: Batch calculating performance for all processed funds")
        all_fund_codes_processed = new_fund_codes + update_fund_codes
        all_fund_nav_data = batch_fetch_nav_history(all_fund_codes_processed, db_path, max_workers, use_cache)

        all_performance = batch_calculate_performance(all_fund_nav_data, db_path, max_workers)

        if all_performance:
            saved = save_performance(all_performance, db_path)
            total_records += saved
            logger.info(f"Saved {saved} performance records")

        elapsed_time = int(time.time() - start_time)

        record_update_history(
            update_type="BATCH",
            table_name="ALL",
            status="SUCCESS",
            record_count=total_records,
            new_record_count=new_records,
            elapsed_time=elapsed_time,
            error_message="",
            fund_codes=all_fund_codes_processed,
            db_path=db_path
        )

        logger.info(f"Batch import completed: {total_records} records, {new_records} new, {elapsed_time}s")

        return {
            "success": True,
            "total_records": total_records,
            "new_records": new_records,
            "elapsed_time": elapsed_time,
            "processed_funds": len(all_fund_codes_processed)
        }

    except Exception as e:
        logger.error(f"Error in batch import: {e}")
        elapsed_time = int(time.time() - start_time)
        record_update_history(
            update_type="BATCH",
            table_name="ALL",
            status="FAILED",
            record_count=0,
            new_record_count=0,
            elapsed_time=elapsed_time,
            error_message=str(e),
            db_path=db_path
        )
        return {"success": False, "error": str(e)}
    finally:
        release_update_lock()


def show_status(db_path: str = DATABASE_PATH) -> Dict[str, Any]:
    try:
        with get_db_connection(db_path) as conn:
            cursor = conn.cursor()

            cursor.execute("SELECT COUNT(*) FROM fund_basic_info")
            fund_count = cursor.fetchone()[0]

            cursor.execute("SELECT COUNT(*) FROM fund_nav_history")
            nav_count = cursor.fetchone()[0]

            cursor.execute("SELECT COUNT(*) FROM fund_performance")
            perf_count = cursor.fetchone()[0]

            cursor.execute("SELECT COUNT(*) FROM fund_manager")
            manager_count = cursor.fetchone()[0]

            cursor.execute("SELECT COUNT(*) FROM fund_corporate_actions")
            action_count = cursor.fetchone()[0]

            cursor.execute("SELECT COUNT(*) FROM fund_asset_scale")
            scale_count = cursor.fetchone()[0]

            latest_update = get_latest_update_history(db_path)

            cursor.execute("PRAGMA integrity_check")
            integrity = cursor.fetchone()[0]

            return {
                "database_exists": os.path.exists(db_path),
                "fund_count": fund_count,
                "nav_count": nav_count,
                "performance_count": perf_count,
                "manager_count": manager_count,
                "corporate_actions_count": action_count,
                "asset_scale_count": scale_count,
                "latest_update": dict(latest_update) if latest_update else None,
                "integrity_check": integrity,
                "update_lock_exists": os.path.exists(UPDATE_LOCK_FILE)
            }
    except Exception as e:
        logger.error(f"Error showing status: {e}")
        return {"error": str(e)}


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Missing command"}, ensure_ascii=False))
        sys.exit(1)

    command = sys.argv[1]

    if command == "create":
        success = create_database()
        print(json.dumps({"success": success}, ensure_ascii=False))

    elif command == "repair":
        success = repair_database()
        print(json.dumps({"success": success}, ensure_ascii=False))

    elif command == "get_fund_list":
        limit = int(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2].isdigit() else None
        only_a_share = sys.argv[3].lower() == "true" if len(sys.argv) > 3 else False
        fund_codes = fetch_fund_list_from_akshare(use_cache=True, limit=limit, only_a_share=only_a_share)
        print(json.dumps(fund_codes, ensure_ascii=False))

    elif command == "import_fund_data":
        max_workers = int(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2].isdigit() else None
        limit = int(sys.argv[3]) if len(sys.argv) > 3 else None
        result = import_fund_data(max_workers=max_workers, limit=limit)
        print(json.dumps(result, ensure_ascii=False))

    elif command == "update_single_fund":
        fund_code = sys.argv[2] if len(sys.argv) > 2 else None
        if not fund_code:
            print(json.dumps({"error": "Missing fund_code"}, ensure_ascii=False))
            sys.exit(1)
        start_date = sys.argv[3] if len(sys.argv) > 3 else None
        end_date = sys.argv[4] if len(sys.argv) > 4 else None
        total_records, new_records = update_single_fund(fund_code, start_date, end_date)
        print(json.dumps({"total_records": total_records, "new_records": new_records}, ensure_ascii=False))

    elif command == "fetch_fund_performance":
        fund_code = sys.argv[2] if len(sys.argv) > 2 else None
        if not fund_code:
            print(json.dumps({"error": "Missing fund_code"}, ensure_ascii=False))
            sys.exit(1)
        is_first_insert = sys.argv[3].lower() == "true" if len(sys.argv) > 3 else False
        use_cache = sys.argv[4].lower() == "true" if len(sys.argv) > 4 else True
        performance = fetch_fund_performance(fund_code, is_first_insert=is_first_insert, use_cache=use_cache)
        print(json.dumps(performance, ensure_ascii=False))

    elif command == "status":
        status = show_status()
        print(json.dumps(status, ensure_ascii=False))

    elif command == "clear_cache":
        clear_expired_cache()
        print(json.dumps({"success": True}, ensure_ascii=False))

    elif command == "recalculate_performance":
        import multiprocessing as mp
        from functools import partial

        def process_fund(code):
            records, new_count = recalculate_performance_only(code)
            return code, records, new_count

        fund_codes = get_all_fund_codes_fromDb()
        total = len(fund_codes)
        logger.info(f"Recalculating performance for {total} funds")

        results = []
        with mp.Pool(processes=15) as pool:
            for i, (code, records, new_count) in enumerate(pool.imap(process_fund, fund_codes), 1):
                results.append((code, records, new_count))
                if i % 100 == 0:
                    logger.info(f"Progress: {i}/{total}")

        total_records = sum(r[1] for r in results)
        logger.info(f"Completed: {total} funds, {total_records} records")
        print(json.dumps({"success": True, "total_funds": total, "total_records": total_records}, ensure_ascii=False))

    else:
        print(json.dumps({"error": f"Unknown command: {command}"}, ensure_ascii=False))
        sys.exit(1)
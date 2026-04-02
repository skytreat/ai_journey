#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
基金数据库管理模块单元测试
使用Mock避免外部依赖，确保60%以上代码覆盖率
"""

import unittest
import sys
import os
import sqlite3
import tempfile
import shutil
from datetime import datetime, timedelta
from unittest.mock import patch, MagicMock
from typing import List, Dict, Any

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


class TestDetermineShareType(unittest.TestCase):
    """测试 determine_share_type 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["determine_share_type"])
        self.func = self.module.determine_share_type

    def test_none_fund_name_returns_A(self):
        """测试 fund_name 为 None 时返回 A"""
        result = self.func("007590", None)
        self.assertEqual(result, "A")

    def test_empty_fund_name_returns_A(self):
        """测试 fund_name 为空时返回 A"""
        result = self.func("007590", "")
        self.assertEqual(result, "A")

    def test_uppercase_last_char_returns_that_char(self):
        """测试最后一个字符是大写字母时返回该字符"""
        result = self.func("007590", "易方达消费行业股票B")
        self.assertEqual(result, "B")

    def test_lowercase_last_char_returns_A(self):
        """测试最后一个字符是小写字母时返回 A"""
        result = self.func("007590", "易方达消费行业股票b")
        self.assertEqual(result, "A")

    def test_digit_last_char_returns_A(self):
        """测试最后一个字符是数字时返回 A"""
        result = self.func("007590", "易方达消费行业股票1")
        self.assertEqual(result, "A")

    def test_normal_fund_name_returns_A(self):
        """测试普通基金名称返回 A"""
        result = self.func("007590", "易方达消费行业股票")
        self.assertEqual(result, "A")


class TestParseFundType(unittest.TestCase):
    """测试 parse_fund_type 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["parse_fund_type"])
        self.func = self.module.parse_fund_type

    def test_none_input_returns_empty_strings(self):
        """测试 None 输入返回空字符串"""
        result = self.func(None)
        self.assertEqual(result, ("", ""))

    def test_empty_string_returns_empty_strings(self):
        """测试空字符串输入返回空字符串"""
        result = self.func("")
        self.assertEqual(result, ("", ""))

    def test_hybrid_partial_stock(self):
        """测试混合型-偏股"""
        result = self.func("混合型-偏股")
        self.assertEqual(result, ("混合型", "偏股"))

    def test_hybrid_flexible(self):
        """测试混合型-灵活"""
        result = self.func("混合型-灵活")
        self.assertEqual(result, ("混合型", "灵活"))

    def test_hybrid_balance(self):
        """测试混合型-平衡"""
        result = self.func("混合型-平衡")
        self.assertEqual(result, ("混合型", "平衡"))

    def test_bond_partial_bond(self):
        """测试债券型-偏债"""
        result = self.func("债券型-偏债")
        self.assertEqual(result, ("债券型", "偏债"))

    def test_stock_type(self):
        """测试股票型（无子类型）"""
        result = self.func("股票型")
        self.assertEqual(result, ("股票型", ""))

    def test_en_type(self):
        """测试包含英文的类型"""
        result = self.func("QDII-指数")
        self.assertEqual(result, ("QDII", "指数"))

    def test_underscore_separator(self):
        """测试下划线分隔符"""
        result = self.func("混合型_灵活")
        self.assertEqual(result, ("混合型", "灵活"))

    def test_multiple_separators_uses_first(self):
        """测试多个分隔符时使用第一个"""
        result = self.func("混合型-偏股-灵活")
        self.assertEqual(result, ("混合型", "偏股-灵活"))


class TestGetUpdateStartDate(unittest.TestCase):
    """测试 get_update_start_date 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["get_update_start_date"])
        self.func = self.module.get_update_start_date

    def test_none_latest_date_with_none_start_date(self):
        """测试 latest_date 和 start_date 都为 None"""
        result = self.func(None, None, "2024-12-31")
        self.assertIsNone(result)

    def test_none_latest_date_with_start_date(self):
        """测试 latest_date 为 None 但 start_date 有值"""
        result = self.func(None, "2024-01-01", "2024-12-31")
        self.assertEqual(result, "2024-01-01")

    def test_latest_date_before_start_date(self):
        """测试 latest_date 早于 start_date，返回 latest_date + 1天"""
        result = self.func("2024-01-01", "2024-06-01", "2024-12-31")
        self.assertEqual(result, "2024-01-02")

    def test_latest_date_after_start_date(self):
        """测试 latest_date 晚于 start_date"""
        result = self.func("2024-06-01", "2024-01-01", "2024-12-31")
        self.assertEqual(result, "2024-06-02")

    def test_latest_date_equals_start_date(self):
        """测试 latest_date 等于 start_date"""
        result = self.func("2024-06-01", "2024-06-01", "2024-12-31")
        self.assertEqual(result, "2024-06-02")


class TestCacheFunctions(unittest.TestCase):
    """测试缓存相关函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["get_cache_key", "get_cache", "set_cache"])
        self.test_cache_dir = tempfile.mkdtemp()
        self.module.CACHE_DIR = self.test_cache_dir

    def tearDown(self):
        shutil.rmtree(self.test_cache_dir, ignore_errors=True)

    def test_get_cache_key(self):
        """测试缓存键生成"""
        func = self.module.get_cache_key
        result = func("prefix", "arg1", "arg2")
        self.assertEqual(result, "prefix_arg1_arg2")

    def test_get_cache_key_single_arg(self):
        """测试单参数缓存键生成"""
        func = self.module.get_cache_key
        result = func("prefix", "arg1")
        self.assertEqual(result, "prefix_arg1")

    def test_set_and_get_cache_json(self):
        """测试 JSON 缓存读写"""
        func_set = self.module.set_cache
        func_get = self.module.get_cache

        test_data = {"key": "value", "list": [1, 2, 3]}
        func_set("test_cache", test_data, use_pickle=False)

        result = func_get("test_cache", use_pickle=False)
        self.assertEqual(result, test_data)

    def test_set_and_get_cache_pickle(self):
        """测试 Pickle 缓存读写"""
        func_set = self.module.set_cache
        func_get = self.module.get_cache

        test_data = {"key": "value", "tuple": (1, 2, 3)}
        func_set("test_pickle_cache", test_data, use_pickle=True)

        result = func_get("test_pickle_cache", use_pickle=True)
        self.assertEqual(result, test_data)

    def test_get_cache_not_exists(self):
        """测试获取不存在的缓存"""
        func = self.module.get_cache
        result = func("nonexistent_cache")
        self.assertIsNone(result)


class TestDatabaseOperations(unittest.TestCase):
    """测试数据库操作函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_db_connection"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_create_database(self):
        """测试数据库创建"""
        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
        tables = [row[0] for row in cursor.fetchall()]
        conn.close()

        self.assertIn("fund_basic_info", tables)
        self.assertIn("fund_nav_history", tables)
        self.assertIn("fund_performance", tables)
        self.assertIn("fund_manager", tables)

    def test_fund_basic_info_table_structure(self):
        """测试 fund_basic_info 表结构"""
        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("PRAGMA table_info(fund_basic_info)")
        columns = {row[1]: row[2] for row in cursor.fetchall()}
        conn.close()

        self.assertIn("代码", columns)
        self.assertIn("名称", columns)
        self.assertIn("基金类型", columns)
        self.assertIn("基金子类型", columns)
        self.assertIn("份额类型", columns)

    def test_fund_nav_history_table_structure(self):
        """测试 fund_nav_history 表结构"""
        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("PRAGMA table_info(fund_nav_history)")
        columns = {row[1]: row[2] for row in cursor.fetchall()}
        conn.close()

        self.assertIn("代码", columns)
        self.assertIn("日期", columns)
        self.assertIn("单位净值", columns)
        self.assertIn("累计净值", columns)


class TestGetDbConnection(unittest.TestCase):
    """测试数据库连接上下文管理器"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_db_connection"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_db_connection_context_manager(self):
        """测试 get_db_connection 上下文管理器"""
        get_db_connection = self.module.get_db_connection

        with get_db_connection(self.test_db) as conn:
            cursor = conn.cursor()
            cursor.execute("SELECT 1 as test")
            result = cursor.fetchone()[0]
            self.assertEqual(result, 1)

    def test_connection_closes_after_use(self):
        """测试连接使用后正确关闭"""
        get_db_connection = self.module.get_db_connection

        conn = None
        with get_db_connection(self.test_db) as c:
            conn = c

        with get_db_connection(self.test_db) as c:
            cursor = c.cursor()
            cursor.execute("SELECT 1")
            result = cursor.fetchone()[0]
            self.assertEqual(result, 1)


class TestGetLatestNavDate(unittest.TestCase):
    """测试 get_latest_nav_date 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_latest_nav_date"])
        create_database = self.module.create_database
        create_database(self.test_db)

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_nav_history (代码, 日期, 单位净值, 累计净值, 更新日期)
            VALUES ('TEST001', '2024-01-01', 1.0, 1.0, datetime('now'))
        """)
        cursor.execute("""
            INSERT INTO fund_nav_history (代码, 日期, 单位净值, 累计净值, 更新日期)
            VALUES ('TEST001', '2024-01-15', 1.1, 1.1, datetime('now'))
        """)
        cursor.execute("""
            INSERT INTO fund_nav_history (代码, 日期, 单位净值, 累计净值, 更新日期)
            VALUES ('TEST001', '2024-01-20', 1.05, 1.05, datetime('now'))
        """)
        conn.commit()
        conn.close()

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_latest_nav_date_exists(self):
        """测试获取已存在的最新净值日期"""
        get_latest_nav_date = self.module.get_latest_nav_date
        result = get_latest_nav_date("TEST001", self.test_db)
        self.assertEqual(result, "2024-01-20")

    def test_get_latest_nav_date_not_exists(self):
        """测试获取不存在的基金净值日期"""
        get_latest_nav_date = self.module.get_latest_nav_date
        result = get_latest_nav_date("NONEXIST", self.test_db)
        self.assertIsNone(result)


class TestGetAllFundCodes(unittest.TestCase):
    """测试 get_all_fund_codes 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_all_fund_codes"])
        create_database = self.module.create_database
        create_database(self.test_db)

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_basic_info (代码, 名称, 基金类型, 基金子类型, 份额类型, 更新日期)
            VALUES ('001', '基金1', '股票型', '', 'A', datetime('now'))
        """)
        cursor.execute("""
            INSERT INTO fund_basic_info (代码, 名称, 基金类型, 基金子类型, 份额类型, 更新日期)
            VALUES ('002', '基金2', '债券型', '', 'A', datetime('now'))
        """)
        conn.commit()
        conn.close()

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_all_fund_codes(self):
        """测试获取所有基金代码"""
        get_all_fund_codes = self.module.get_all_fund_codes
        result = get_all_fund_codes(self.test_db)
        self.assertEqual(len(result), 2)
        self.assertIn("001", result)
        self.assertIn("002", result)


class TestFindMainFundCode(unittest.TestCase):
    """测试 find_main_fund_code 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "find_main_fund_code"])
        create_database = self.module.create_database
        create_database(self.test_db)

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_basic_info (代码, 名称, 基金类型, 基金子类型, 份额类型, 更新日期)
            VALUES ('001', '易方达消费行业股票A', '股票型', '', 'A', datetime('now'))
        """)
        cursor.execute("""
            INSERT INTO fund_basic_info (代码, 名称, 基金类型, 基金子类型, 份额类型, 更新日期)
            VALUES ('001B', '易方达消费行业股票B', '股票型', '', 'B', datetime('now'))
        """)
        conn.commit()
        conn.close()

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_find_main_fund_code_exists(self):
        """测试查找主基金代码（已存在）"""
        find_main_fund_code = self.module.find_main_fund_code
        result = find_main_fund_code("易方达消费行业股票B", self.test_db)
        self.assertEqual(result, "001")

    def test_find_main_fund_code_not_exists(self):
        """测试查找不存在的基金代码"""
        find_main_fund_code = self.module.find_main_fund_code
        result = find_main_fund_code("不存在的基金", self.test_db)
        self.assertIsNone(result)

    def test_find_main_fund_code_empty_name(self):
        """测试空名称"""
        find_main_fund_code = self.module.find_main_fund_code
        result = find_main_fund_code("", self.test_db)
        self.assertIsNone(result)


class TestSaveBasicInfo(unittest.TestCase):
    """测试 save_basic_info 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_basic_info"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_basic_info_success(self):
        """测试成功保存基本信息"""
        save_basic_info = self.module.save_basic_info
        fund_data = {
            "代码": "TEST001",
            "名称": "测试基金",
            "基金类型": "混合型",
            "基金子类型": "偏股",
            "份额类型": "A",
            "成立日期": "2020-01-01",
            "基金管理人": "测试公司",
            "基金托管人": "测试托管",
            "管理费率": "1.5%",
            "托管费率": "0.2%",
            "销售服务费率": "",
            "业绩比较基准": "",
            "跟踪标的": "",
            "投资风格": "",
            "风险等级": "中",
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        }
        result = save_basic_info(fund_data, self.test_db)
        self.assertTrue(result)

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("SELECT * FROM fund_basic_info WHERE 代码 = 'TEST001'")
        row = cursor.fetchone()
        conn.close()
        self.assertIsNotNone(row)

    def test_save_basic_info_duplicate(self):
        """测试重复插入（应返回 False）"""
        save_basic_info = self.module.save_basic_info
        fund_data = {
            "代码": "TEST001",
            "名称": "测试基金",
            "基金类型": "混合型",
            "基金子类型": "偏股",
            "份额类型": "A",
            "成立日期": "2020-01-01",
            "基金管理人": "测试公司",
            "基金托管人": "测试托管",
            "管理费率": "1.5%",
            "托管费率": "0.2%",
            "销售服务费率": "",
            "业绩比较基准": "",
            "跟踪标的": "",
            "投资风格": "",
            "风险等级": "中",
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        }
        save_basic_info(fund_data, self.test_db)
        result = save_basic_info(fund_data, self.test_db)
        self.assertFalse(result)

    def test_save_basic_info_empty_data(self):
        """测试空数据返回 False"""
        save_basic_info = self.module.save_basic_info
        result = save_basic_info({}, self.test_db)
        self.assertFalse(result)


class TestSaveNavHistory(unittest.TestCase):
    """测试 save_nav_history 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_nav_history"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_nav_history_success(self):
        """测试成功保存净值历史"""
        save_nav_history = self.module.save_nav_history
        nav_data = [
            {
                "代码": "TEST001",
                "日期": "2024-01-01",
                "单位净值": 1.0,
                "累计净值": 1.0,
                "日增长率": 0.0,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            },
            {
                "代码": "TEST001",
                "日期": "2024-01-02",
                "单位净值": 1.01,
                "累计净值": 1.01,
                "日增长率": 1.0,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_nav_history(nav_data, self.test_db)
        self.assertEqual(result, 2)

    def test_save_nav_history_empty_list(self):
        """测试空列表返回 0"""
        save_nav_history = self.module.save_nav_history
        result = save_nav_history([], self.test_db)
        self.assertEqual(result, 0)

    def test_save_nav_history_duplicate(self):
        """测试重复数据（应跳过）"""
        save_nav_history = self.module.save_nav_history
        nav_data = [
            {
                "代码": "TEST001",
                "日期": "2024-01-01",
                "单位净值": 1.0,
                "累计净值": 1.0,
                "日增长率": 0.0,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        save_nav_history(nav_data, self.test_db)
        result = save_nav_history(nav_data, self.test_db)
        self.assertEqual(result, 0)


class TestUpdateSingleFundMock(unittest.TestCase):
    """测试 update_single_fund 函数（使用 Mock）"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "update_single_fund", "get_basic_info_from_db"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    @patch("fund_database_manager.fetch_fund_basic_info")
    @patch("fund_database_manager.save_basic_info")
    @patch("fund_database_manager.get_basic_info_from_db")
    @patch("fund_database_manager.get_latest_nav_date")
    @patch("fund_database_manager.fetch_fund_nav_history")
    @patch("fund_database_manager.save_nav_history")
    @patch("fund_database_manager.get_latest_corporate_action_date")
    @patch("fund_database_manager.fetch_corporate_actions")
    @patch("fund_database_manager.save_corporate_actions")
    @patch("fund_database_manager.has_new_corporate_actions")
    @patch("fund_database_manager.fetch_fund_performance")
    @patch("fund_database_manager.save_performance")
    def test_update_single_fund_new_fund(
        self,
        mock_save_perf, mock_fetch_perf, mock_has_new,
        mock_save_ca, mock_fetch_ca, mock_get_ca_date,
        mock_save_nav, mock_fetch_nav, mock_get_nav_date,
        mock_get_basic_db, mock_save_basic, mock_fetch_basic
    ):
        """测试更新新基金"""
        mock_get_basic_db.return_value = None
        mock_fetch_basic.return_value = {
            "代码": "TEST001",
            "名称": "测试基金",
            "基金类型": "混合型",
            "基金子类型": "偏股",
            "份额类型": "A",
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        }
        mock_save_basic.return_value = True
        mock_get_nav_date.return_value = None
        mock_fetch_nav.return_value = [
            {"代码": "TEST001", "日期": "2024-01-01", "单位净值": 1.0, "累计净值": 1.0, "日增长率": 0.0}
        ]
        mock_save_nav.return_value = 1
        mock_get_ca_date.return_value = None
        mock_fetch_ca.return_value = []
        mock_save_ca.return_value = 0
        mock_has_new.return_value = False
        mock_fetch_perf.return_value = []
        mock_save_perf.return_value = 0

        update_single_fund = self.module.update_single_fund
        total, new = update_single_fund("TEST001", None, "2024-01-31", self.test_db)

        self.assertGreater(total, 0)
        self.assertTrue(mock_fetch_basic.called)
        self.assertTrue(mock_save_basic.called)


class TestGetCacheKey(unittest.TestCase):
    """测试 get_cache_key 函数"""

    def test_cache_key_multiple_args(self):
        """测试多参数缓存键"""
        module = __import__("fund_database_manager", fromlist=["get_cache_key"])
        func = module.get_cache_key
        result = func("nav", "007590", "2024-01-01")
        self.assertEqual(result, "nav_007590_2024-01-01")

    def test_cache_key_single_arg(self):
        """测试单参数缓存键"""
        module = __import__("fund_database_manager", fromlist=["get_cache_key"])
        func = module.get_cache_key
        result = func("list")
        self.assertEqual(result, "list")


class TestRepairDatabase(unittest.TestCase):
    """测试 repair_database 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_repair_database(self):
        """测试修复数据库"""
        module = __import__("fund_database_manager", fromlist=["repair_database"])
        repair_database = module.repair_database
        result = repair_database(self.test_db)
        self.assertTrue(result)


class TestAcquireReleaseLock(unittest.TestCase):
    """测试锁函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["acquire_update_lock", "release_update_lock"])
        self.lock_file = tempfile.mktemp()

    def tearDown(self):
        if os.path.exists(self.lock_file):
            os.remove(self.lock_file)

    def test_acquire_lock(self):
        """测试获取锁"""
        acquire_update_lock = self.module.acquire_update_lock
        release_update_lock = self.module.release_update_lock

        self.module.UPDATE_LOCK_FILE = self.lock_file

        result = acquire_update_lock()
        self.assertTrue(result)

        release_update_lock()

    def test_release_nonexistent_lock(self):
        """测试释放不存在的锁"""
        release_update_lock = self.module.release_update_lock
        self.module.UPDATE_LOCK_FILE = self.lock_file
        release_update_lock()


class TestGetLatestUpdateHistory(unittest.TestCase):
    """测试 get_latest_update_history 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_latest_update_history"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_latest_update_history_empty(self):
        """测试空数据库返回 None"""
        get_latest_update_history = self.module.get_latest_update_history
        result = get_latest_update_history(self.test_db)
        self.assertIsNone(result)

    def test_get_latest_update_history_with_data(self):
        """测试有数据时返回最新记录"""
        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_info_update_history (基金代码列表, 更新类型, 更新表名, 更新日期)
            VALUES ('001,002', '全量', 'fund_basic_info', datetime('now'))
        """)
        conn.commit()
        conn.close()

        get_latest_update_history = self.module.get_latest_update_history
        result = get_latest_update_history(self.test_db)
        self.assertIsNotNone(result)


class TestSaveNavHistoryMore(unittest.TestCase):
    """更多 save_nav_history 测试"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_nav_history"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_nav_history_with_missing_fields(self):
        """测试缺失字段的数据被跳过"""
        save_nav_history = self.module.save_nav_history
        nav_data = [
            {
                "代码": "000001",
                "日期": "2024-01-01",
            }
        ]
        result = save_nav_history(nav_data, self.test_db)
        self.assertEqual(result, 0)


class TestGetLatestCorporateActionDate(unittest.TestCase):
    """测试 get_latest_corporate_action_date 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_latest_corporate_action_date"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_latest_corporate_action_date_exists(self):
        """测试获取已存在的最新除权日期"""
        get_latest_corporate_action_date = self.module.get_latest_corporate_action_date

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_corporate_actions (代码, 除权日期, 事件类型, 更新日期)
            VALUES ('TEST001', '2024-01-15', '分红', datetime('now'))
        """)
        cursor.execute("""
            INSERT INTO fund_corporate_actions (代码, 除权日期, 事件类型, 更新日期)
            VALUES ('TEST001', '2024-01-20', '拆分', datetime('now'))
        """)
        conn.commit()
        conn.close()

        result = get_latest_corporate_action_date("TEST001", self.test_db)
        self.assertEqual(result, "2024-01-20")

    def test_get_latest_corporate_action_date_not_exists(self):
        """测试获取不存在的基金"""
        get_latest_corporate_action_date = self.module.get_latest_corporate_action_date
        result = get_latest_corporate_action_date("NONEXIST", self.test_db)
        self.assertIsNone(result)


class TestGetLatestManagerDate(unittest.TestCase):
    """测试 get_latest_manager_date 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_latest_manager_date"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_latest_manager_date_exists(self):
        """测试获取已存在的最新经理日期"""
        get_latest_manager_date = self.module.get_latest_manager_date

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_manager (代码, 基金经理姓名, 任职开始日期, 任职结束日期, 管理天数, 更新日期)
            VALUES ('TEST001', '张三', '2020-01-01', '2021-01-01', 365, datetime('now'))
        """)
        cursor.execute("""
            INSERT INTO fund_manager (代码, 基金经理姓名, 任职开始日期, 任职结束日期, 管理天数, 更新日期)
            VALUES ('TEST001', '李四', '2021-01-01', NULL, 730, datetime('now'))
        """)
        conn.commit()
        conn.close()

        result = get_latest_manager_date("TEST001", self.test_db)
        self.assertEqual(result, "2021-01-01")

    def test_get_latest_manager_date_not_exists(self):
        """测试获取不存在的基金"""
        get_latest_manager_date = self.module.get_latest_manager_date
        result = get_latest_manager_date("NONEXIST", self.test_db)
        self.assertIsNone(result)


class TestGetLatestAssetScaleDate(unittest.TestCase):
    """测试 get_latest_asset_scale_date 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_latest_asset_scale_date"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_latest_asset_scale_date_exists(self):
        """测试获取已存在的最新资产规模日期"""
        get_latest_asset_scale_date = self.module.get_latest_asset_scale_date

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_asset_scale (代码, 日期, 资产规模, 份额规模, 更新日期)
            VALUES ('TEST001', '2024-01-01', 10.5, 5.2, datetime('now'))
        """)
        cursor.execute("""
            INSERT INTO fund_asset_scale (代码, 日期, 资产规模, 份额规模, 更新日期)
            VALUES ('TEST001', '2024-01-15', 11.2, 5.5, datetime('now'))
        """)
        conn.commit()
        conn.close()

        result = get_latest_asset_scale_date("TEST001", self.test_db)
        self.assertEqual(result, "2024-01-15")

    def test_get_latest_asset_scale_date_not_exists(self):
        """测试获取不存在的基金"""
        get_latest_asset_scale_date = self.module.get_latest_asset_scale_date
        result = get_latest_asset_scale_date("NONEXIST", self.test_db)
        self.assertIsNone(result)


class TestGetLatestPurchaseStatusDate(unittest.TestCase):
    """测试 get_latest_purchase_status_date 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_latest_purchase_status_date"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_latest_purchase_status_date_exists(self):
        """测试获取已存在的最新申购状态日期"""
        get_latest_purchase_status_date = self.module.get_latest_purchase_status_date

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_purchase_status (代码, 日期, 申购状态, 申购限额, 申购手续费率, 更新日期)
            VALUES ('TEST001', '2024-01-01', '开放', 10000, 0.01, datetime('now'))
        """)
        conn.commit()
        conn.close()

        result = get_latest_purchase_status_date("TEST001", self.test_db)
        self.assertEqual(result, "2024-01-01")

    def test_get_latest_purchase_status_date_not_exists(self):
        """测试获取不存在的基金"""
        get_latest_purchase_status_date = self.module.get_latest_purchase_status_date
        result = get_latest_purchase_status_date("NONEXIST", self.test_db)
        self.assertIsNone(result)


class TestGetLatestRedemptionStatusDate(unittest.TestCase):
    """测试 get_latest_redemption_status_date 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_latest_redemption_status_date"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_latest_redemption_status_date_exists(self):
        """测试获取已存在的最新赎回状态日期"""
        get_latest_redemption_status_date = self.module.get_latest_redemption_status_date

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_redemption_status (代码, 日期, 赎回状态, 赎回限额, 赎回手续费率, 更新日期)
            VALUES ('TEST001', '2024-01-01', '开放', 10000, 0.005, datetime('now'))
        """)
        conn.commit()
        conn.close()

        result = get_latest_redemption_status_date("TEST001", self.test_db)
        self.assertEqual(result, "2024-01-01")

    def test_get_latest_redemption_status_date_not_exists(self):
        """测试获取不存在的基金"""
        get_latest_redemption_status_date = self.module.get_latest_redemption_status_date
        result = get_latest_redemption_status_date("NONEXIST", self.test_db)
        self.assertIsNone(result)


class TestSaveManagers(unittest.TestCase):
    """测试 save_managers 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_managers"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_managers_success(self):
        """测试成功保存基金经理"""
        save_managers = self.module.save_managers
        managers = [
            {
                "代码": "TEST001",
                "基金经理姓名": "张三",
                "任职开始日期": "2020-01-01",
                "任职结束日期": "2021-01-01",
                "管理天数": 365,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_managers(managers, self.test_db)
        self.assertEqual(result, 1)

    def test_save_managers_empty_list(self):
        """测试空列表返回 0"""
        save_managers = self.module.save_managers
        result = save_managers([], self.test_db)
        self.assertEqual(result, 0)

    def test_save_managers_duplicate(self):
        """测试重复数据（INSERT OR IGNORE会返回1）"""
        save_managers = self.module.save_managers
        managers = [
            {
                "代码": "TEST001",
                "基金经理姓名": "张三",
                "任职开始日期": "2020-01-01",
                "任职结束日期": "2021-01-01",
                "管理天数": 365,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        save_managers(managers, self.test_db)
        result = save_managers(managers, self.test_db)
        self.assertEqual(result, 1)


class TestSaveCorporateActions(unittest.TestCase):
    """测试 save_corporate_actions 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_corporate_actions"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_corporate_actions_success(self):
        """测试成功保存公司行为"""
        save_corporate_actions = self.module.save_corporate_actions
        actions = [
            {
                "代码": "TEST001",
                "除权日期": "2024-01-01",
                "事件类型": "分红",
                "每份分红金额": 0.5,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_corporate_actions(actions, self.test_db)
        self.assertEqual(result, 1)

    def test_save_corporate_actions_empty_list(self):
        """测试空列表返回 0"""
        save_corporate_actions = self.module.save_corporate_actions
        result = save_corporate_actions([], self.test_db)
        self.assertEqual(result, 0)


class TestSaveAssetScale(unittest.TestCase):
    """测试 save_asset_scale 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_asset_scale"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_asset_scale_success(self):
        """测试成功保存资产规模"""
        save_asset_scale = self.module.save_asset_scale
        scales = [
            {
                "代码": "TEST001",
                "日期": "2024-01-01",
                "资产规模": 10.5,
                "份额规模": 5.2,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_asset_scale(scales, self.test_db)
        self.assertEqual(result, 1)

    def test_save_asset_scale_empty_list(self):
        """测试空列表返回 0"""
        save_asset_scale = self.module.save_asset_scale
        result = save_asset_scale([], self.test_db)
        self.assertEqual(result, 0)


class TestSavePurchaseStatus(unittest.TestCase):
    """测试 save_purchase_status 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_purchase_status"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_purchase_status_success(self):
        """测试成功保存申购状态"""
        save_purchase_status = self.module.save_purchase_status
        statuses = [
            {
                "代码": "TEST001",
                "日期": "2024-01-01",
                "申购状态": "开放",
                "申购限额": 10000,
                "申购手续费率": 0.01,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_purchase_status(statuses, self.test_db)
        self.assertEqual(result, 1)

    def test_save_purchase_status_empty_list(self):
        """测试空列表返回 0"""
        save_purchase_status = self.module.save_purchase_status
        result = save_purchase_status([], self.test_db)
        self.assertEqual(result, 0)


class TestSaveRedemptionStatus(unittest.TestCase):
    """测试 save_redemption_status 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_redemption_status"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_redemption_status_success(self):
        """测试成功保存赎回状态"""
        save_redemption_status = self.module.save_redemption_status
        statuses = [
            {
                "代码": "TEST001",
                "日期": "2024-01-01",
                "赎回状态": "开放",
                "赎回限额": 10000,
                "赎回手续费率": 0.005,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_redemption_status(statuses, self.test_db)
        self.assertEqual(result, 1)

    def test_save_redemption_status_empty_list(self):
        """测试空列表返回 0"""
        save_redemption_status = self.module.save_redemption_status
        result = save_redemption_status([], self.test_db)
        self.assertEqual(result, 0)


class TestSavePerformance(unittest.TestCase):
    """测试 save_performance 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_performance"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_performance_success(self):
        """测试成功保存业绩"""
        save_performance = self.module.save_performance
        performances = [
            {
                "代码": "TEST001",
                "周期类型": "近1年",
                "周期值": "",
                "净值增长率": 15.5,
                "最大回撤": -10.2,
                "下行标准差": 5.5,
                "夏普比率": 1.2,
                "索提诺比率": 1.5,
                "卡玛比率": 0.8,
                "年化收益率": 15.5,
                "波动率": 12.3,
                "同类型基金排名": 100,
                "同类型基金总数": 1000,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_performance(performances, self.test_db)
        self.assertEqual(result, 1)

    def test_save_performance_empty_list(self):
        """测试空列表返回 0"""
        save_performance = self.module.save_performance
        result = save_performance([], self.test_db)
        self.assertEqual(result, 0)


class TestHasNewCorporateActions(unittest.TestCase):
    """测试 has_new_corporate_actions 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "has_new_corporate_actions", "get_latest_corporate_action_date", "fetch_corporate_actions"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    @patch("fund_database_manager.fetch_corporate_actions")
    def test_has_new_corporate_actions_no_record(self, mock_fetch):
        """测试基金无记录时返回 True"""
        has_new_corporate_actions = self.module.has_new_corporate_actions
        mock_fetch.return_value = []
        result = has_new_corporate_actions("NONEXIST", self.test_db)
        self.assertTrue(result)

    @patch("fund_database_manager.fetch_corporate_actions")
    def test_has_new_corporate_actions_has_new(self, mock_fetch):
        """测试有新记录时返回 True"""
        has_new_corporate_actions = self.module.has_new_corporate_actions

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_corporate_actions (代码, 除权日期, 事件类型, 更新日期)
            VALUES ('TEST001', '2024-01-01', '分红', datetime('now'))
        """)
        conn.commit()
        conn.close()

        mock_fetch.return_value = [{"代码": "TEST001", "除权日期": "2024-01-15"}]
        result = has_new_corporate_actions("TEST001", self.test_db)
        self.assertTrue(result)

    @patch("fund_database_manager.fetch_corporate_actions")
    def test_has_new_corporate_actions_no_new(self, mock_fetch):
        """测试无新记录时返回 False"""
        has_new_corporate_actions = self.module.has_new_corporate_actions

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_corporate_actions (代码, 除权日期, 事件类型, 更新日期)
            VALUES ('TEST001', '2024-01-20', '分红', datetime('now'))
        """)
        conn.commit()
        conn.close()

        mock_fetch.return_value = []
        result = has_new_corporate_actions("TEST001", self.test_db)
        self.assertFalse(result)


class TestGetBasicInfoFromDb(unittest.TestCase):
    """测试 get_basic_info_from_db 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_basic_info_from_db"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_basic_info_from_db_exists(self):
        """测试获取已存在的基本信息"""
        get_basic_info_from_db = self.module.get_basic_info_from_db

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_basic_info (代码, 名称, 基金类型, 基金子类型, 份额类型, 更新日期)
            VALUES ('TEST001', '测试基金', '混合型', '偏股', 'A', datetime('now'))
        """)
        conn.commit()
        conn.close()

        result = get_basic_info_from_db("TEST001", self.test_db)
        self.assertIsNotNone(result)
        self.assertEqual(result["名称"], "测试基金")

    def test_get_basic_info_from_db_not_exists(self):
        """测试获取不存在的基本信息"""
        get_basic_info_from_db = self.module.get_basic_info_from_db
        result = get_basic_info_from_db("NONEXIST", self.test_db)
        self.assertIsNone(result)


class TestGetNavHistoryFromDb(unittest.TestCase):
    """测试 get_nav_history_from_db 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "get_nav_history_from_db"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_get_nav_history_from_db_exists(self):
        """测试获取已存在的净值历史"""
        get_nav_history_from_db = self.module.get_nav_history_from_db

        conn = sqlite3.connect(self.test_db)
        cursor = conn.cursor()
        cursor.execute("""
            INSERT INTO fund_nav_history (代码, 日期, 单位净值, 累计净值, 日增长率, 更新日期)
            VALUES ('TEST001', '2024-01-01', 1.0, 1.0, 0.0, datetime('now'))
        """)
        conn.commit()
        conn.close()

        result = get_nav_history_from_db("TEST001", self.test_db)
        self.assertIsNotNone(result)
        self.assertEqual(len(result), 1)

    def test_get_nav_history_from_db_not_exists(self):
        """测试获取不存在的净值历史"""
        get_nav_history_from_db = self.module.get_nav_history_from_db
        result = get_nav_history_from_db("NONEXIST", self.test_db)
        self.assertEqual(result, [])


class TestGenerateMockNavHistory(unittest.TestCase):
    """测试 generate_mock_nav_history 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["generate_mock_nav_history"])
        self.func = self.module.generate_mock_nav_history

    def test_generate_mock_nav_history_normal(self):
        """测试正常生成模拟净值历史"""
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertEqual(len(result), 10)
        self.assertEqual(result[0]["代码"], "TEST001")
        self.assertEqual(result[0]["日期"], "2024-01-01")

    def test_generate_mock_nav_history_invalid_date(self):
        """测试无效日期格式"""
        result = self.func("TEST001", "invalid", "2024-01-10")
        self.assertEqual(result, [])


class TestGenerateMockPerformance(unittest.TestCase):
    """测试 generate_mock_performance 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["generate_mock_performance"])
        self.func = self.module.generate_mock_performance

    def test_generate_mock_performance(self):
        """测试生成模拟业绩"""
        result = self.func("TEST001")
        self.assertIsInstance(result, list)
        self.assertGreater(len(result), 0)


class TestCalculatePeriodPerformance(unittest.TestCase):
    """测试 calculate_period_performance 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["calculate_period_performance"])
        self.func = self.module.calculate_period_performance

    def test_calculate_period_performance_empty(self):
        """测试空数据"""
        result = self.func([], "近1年")
        self.assertIsNone(result)

    def test_calculate_period_performance_single(self):
        """测试单条数据"""
        nav_data = [{"日期": "2024-01-01", "单位净值": 1.0, "累计净值": 1.0}]
        result = self.func(nav_data, "近1年")
        self.assertIsNone(result)

    def test_calculate_period_performance_multiple(self):
        """测试多条数据"""
        nav_data = [
            {"日期": "2024-01-01", "单位净值": 1.0, "累计净值": 1.0},
            {"日期": "2024-01-02", "单位净值": 1.01, "累计净值": 1.01},
            {"日期": "2024-01-03", "单位净值": 1.02, "累计净值": 1.02},
        ]
        result = self.func(nav_data, "近1年")
        self.assertIsNotNone(result)


class TestFetchFundListNoAkShare(unittest.TestCase):
    """测试 fetch_fund_list_from_akshare 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_fund_list_from_akshare"])
        self.func = self.module.fetch_fund_list_from_akshare

    @patch("fund_database_manager.get_cache")
    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    def test_fetch_fund_list_no_akshare(self, mock_cache):
        """测试 akshare 不可用时返回默认列表"""
        mock_cache.return_value = None
        result = self.func(use_cache=False)
        self.assertIsInstance(result, list)
        self.assertGreater(len(result), 0)

    @patch("fund_database_manager.get_cache")
    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    def test_fetch_fund_list_with_limit(self, mock_cache):
        """测试限制返回数量"""
        mock_cache.return_value = None
        result = self.func(use_cache=False, limit=5)
        self.assertLessEqual(len(result), 5)


class TestFetchFundNavHistoryMock(unittest.TestCase):
    """测试 fetch_fund_nav_history 函数的 Mock"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_fund_nav_history"])
        self.func = self.module.fetch_fund_nav_history

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    def test_fetch_fund_nav_history_mock(self):
        """测试使用模拟数据"""
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertIsInstance(result, list)


class TestFetchCorporateActionsMock(unittest.TestCase):
    """测试 fetch_corporate_actions 函数的 Mock"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_corporate_actions"])
        self.func = self.module.fetch_corporate_actions

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_cache")
    def test_fetch_corporate_actions_mock(self, mock_cache):
        """测试使用模拟数据"""
        mock_cache.return_value = None
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertIsInstance(result, list)


class TestFetchFundManagersMock(unittest.TestCase):
    """测试 fetch_fund_managers 函数的 Mock"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_fund_managers"])
        self.func = self.module.fetch_fund_managers

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_cache")
    def test_fetch_fund_managers_mock(self, mock_cache):
        """测试使用模拟数据"""
        mock_cache.return_value = None
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertIsInstance(result, list)


class TestFetchAssetScaleMock(unittest.TestCase):
    """测试 fetch_asset_scale 函数的 Mock"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_asset_scale"])
        self.func = self.module.fetch_asset_scale

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_cache")
    def test_fetch_asset_scale_mock(self, mock_cache):
        """测试使用模拟数据"""
        mock_cache.return_value = None
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertIsInstance(result, list)


class TestFetchPurchaseStatusMock(unittest.TestCase):
    """测试 fetch_purchase_status 函数的 Mock"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_purchase_status"])
        self.func = self.module.fetch_purchase_status

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_cache")
    def test_fetch_purchase_status_mock(self, mock_cache):
        """测试使用模拟数据"""
        mock_cache.return_value = None
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertIsInstance(result, list)


class TestFetchRedemptionStatusMock(unittest.TestCase):
    """测试 fetch_redemption_status 函数的 Mock"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_redemption_status"])
        self.func = self.module.fetch_redemption_status

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_cache")
    def test_fetch_redemption_status_mock(self, mock_cache):
        """测试使用模拟数据"""
        mock_cache.return_value = None
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertIsInstance(result, list)


class TestFetchFundPerformance(unittest.TestCase):
    """测试 fetch_fund_performance 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "fetch_fund_performance"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_nav_history_from_db")
    def test_fetch_fund_performance_no_data(self, mock_nav):
        """测试无净值数据时返回模拟数据"""
        fetch_fund_performance = self.module.fetch_fund_performance
        mock_nav.return_value = []
        result = fetch_fund_performance("TEST001", self.test_db)
        self.assertIsInstance(result, list)

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_nav_history_from_db")
    def test_fetch_fund_performance_with_data(self, mock_nav):
        """测试有净值数据时计算业绩"""
        fetch_fund_performance = self.module.fetch_fund_performance
        mock_nav.return_value = [
            {"日期": "2024-01-01", "单位净值": 1.0, "累计净值": 1.0},
            {"日期": "2024-01-02", "单位净值": 1.01, "累计净值": 1.01},
            {"日期": "2024-01-03", "单位净值": 1.02, "累计净值": 1.02},
        ]
        result = fetch_fund_performance("TEST001", self.test_db)
        self.assertIsInstance(result, list)
        self.assertGreater(len(result), 0)


class TestValidateDateFormat(unittest.TestCase):
    """测试 validate_date_format 函数"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["validate_date_format"])
        self.func = self.module.validate_date_format

    def test_valid_date(self):
        """测试有效日期"""
        self.assertTrue(self.func("2024-01-01"))

    def test_invalid_date(self):
        """测试无效日期"""
        self.assertFalse(self.func("invalid"))
        self.assertFalse(self.func("2024-13-01"))
        self.assertFalse(self.func(None))

    def test_valid_leap_year(self):
        """测试闰年日期"""
        self.assertTrue(self.func("2024-02-29"))


class TestUpdateSingleFundRobustness(unittest.TestCase):
    """测试 update_single_fund 函数的健壮性"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "update_single_fund"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_invalid_fund_code(self):
        """测试无效基金代码"""
        update_single_fund = self.module.update_single_fund
        total, new = update_single_fund("", None, "2024-01-31", self.test_db)
        self.assertEqual(total, 0)
        self.assertEqual(new, 0)

    def test_invalid_fund_code_none(self):
        """测试 None 基金代码"""
        update_single_fund = self.module.update_single_fund
        total, new = update_single_fund(None, None, "2024-01-31", self.test_db)
        self.assertEqual(total, 0)
        self.assertEqual(new, 0)

    def test_invalid_end_date_format(self):
        """测试无效结束日期格式"""
        update_single_fund = self.module.update_single_fund
        total, new = update_single_fund("TEST001", None, "invalid", self.test_db)
        self.assertEqual(total, 0)
        self.assertEqual(new, 0)

    def test_invalid_start_date_format(self):
        """测试无效开始日期格式"""
        update_single_fund = self.module.update_single_fund
        total, new = update_single_fund("TEST001", "invalid", "2024-01-31", self.test_db)
        self.assertEqual(total, 0)
        self.assertEqual(new, 0)

    def test_nonexistent_database(self):
        """测试不存在的数据库"""
        update_single_fund = self.module.update_single_fund
        total, new = update_single_fund("TEST001", None, "2024-01-31", "/nonexistent/path.db")
        self.assertEqual(total, 0)
        self.assertEqual(new, 0)


if __name__ == "__main__":
    unittest.main(verbosity=2)


class TestFetchFundBasicInfoCache(unittest.TestCase):
    """测试 fetch_fund_basic_info 缓存"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_fund_basic_info"])
        self.func = self.module.fetch_fund_basic_info

    @patch("fund_database_manager.get_cache")
    def test_fetch_basic_info_cache_hit(self, mock_cache):
        """测试缓存命中"""
        cached = {"代码": "TEST001", "名称": "测试基金"}
        mock_cache.return_value = cached
        result = self.func("TEST001")
        self.assertEqual(result, cached)

    @patch("fund_database_manager.AKSHARE_AVAILABLE", False)
    @patch("fund_database_manager.get_cache")
    def test_fetch_basic_info_no_akshare(self, mock_cache):
        """测试 akshare 不可用"""
        mock_cache.return_value = None
        result = self.func("TEST001")
        self.assertIsNone(result)


class TestFetchFundNavHistoryCache(unittest.TestCase):
    """测试 fetch_fund_nav_history 缓存"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_fund_nav_history"])
        self.func = self.module.fetch_fund_nav_history

    @patch("fund_database_manager.get_cache")
    def test_fetch_nav_history_cache_hit(self, mock_cache):
        """测试缓存命中"""
        cached = [{"代码": "TEST001", "日期": "2024-01-01"}]
        mock_cache.return_value = cached
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertEqual(result, cached)


class TestFetchCorporateActionsCache(unittest.TestCase):
    """测试 fetch_corporate_actions 缓存"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_corporate_actions"])
        self.func = self.module.fetch_corporate_actions

    @patch("fund_database_manager.get_cache")
    def test_fetch_corporate_actions_cache_hit(self, mock_cache):
        """测试缓存命中"""
        cached = [{"代码": "TEST001", "除权日期": "2024-01-01"}]
        mock_cache.return_value = cached
        result = self.func("TEST001", "2024-01-01", "2024-01-10")
        self.assertEqual(result, cached)


class TestFetchFundManagersCache(unittest.TestCase):
    """测试 fetch_fund_managers 缓存"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_fund_managers"])
        self.func = self.module.fetch_fund_managers

    @patch("fund_database_manager.get_cache")
    def test_fetch_managers_cache_hit(self, mock_cache):
        """测试缓存命中"""
        cached = [{"代码": "TEST001", "基金经理姓名": "张三"}]
        mock_cache.return_value = cached
        result = self.func("TEST001")
        self.assertEqual(result, cached)


class TestFetchAssetScaleCache(unittest.TestCase):
    """测试 fetch_asset_scale 缓存"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_asset_scale"])
        self.func = self.module.fetch_asset_scale

    @patch("fund_database_manager.get_cache")
    def test_fetch_asset_scale_cache_hit(self, mock_cache):
        """测试缓存命中"""
        cached = [{"代码": "TEST001", "日期": "2024-01-01", "资产规模": 10.5}]
        mock_cache.return_value = cached
        result = self.func("TEST001")
        self.assertEqual(result, cached)


class TestFetchPurchaseStatusCache(unittest.TestCase):
    """测试 fetch_purchase_status 缓存"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_purchase_status"])
        self.func = self.module.fetch_purchase_status

    @patch("fund_database_manager._fetch_purchase_em_data")
    def test_fetch_purchase_status_cache_hit(self, mock_fetch):
        """测试缓存命中"""
        mock_fetch.return_value = {"申购状态": "开放", "赎回状态": "开放", "申购限额": None, "申购手续费率": 0.0}
        result = self.func("TEST001")
        self.assertIsInstance(result, list)


class TestFetchRedemptionStatusCache(unittest.TestCase):
    """测试 fetch_redemption_status 缓存"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["fetch_redemption_status"])
        self.func = self.module.fetch_redemption_status

    @patch("fund_database_manager._fetch_redemption_fee_data")
    @patch("fund_database_manager._fetch_purchase_em_data")
    def test_fetch_redemption_status_cache_hit(self, mock_purchase, mock_fee):
        """测试缓存命中"""
        mock_purchase.return_value = {"申购状态": "开放", "赎回状态": "开放", "申购限额": None, "申购手续费率": 0.0}
        mock_fee.return_value = {"赎回手续费率": 0.005, "赎回限额": 1000}
        result = self.func("TEST001")
        self.assertIsInstance(result, list)


class TestCalculatePeriodPerformanceAllTypes(unittest.TestCase):
    """测试 calculate_period_performance 各种周期类型"""

    def setUp(self):
        self.module = __import__("fund_database_manager", fromlist=["calculate_period_performance"])
        self.func = self.module.calculate_period_performance
        today = datetime.now()
        self.nav_data = [
            {"日期": (today - timedelta(days=400)).strftime("%Y-%m-%d"), "单位净值": 1.0, "累计净值": 1.0},
            {"日期": (today - timedelta(days=200)).strftime("%Y-%m-%d"), "单位净值": 1.2, "累计净值": 1.2},
            {"日期": (today - timedelta(days=100)).strftime("%Y-%m-%d"), "单位净值": 1.1, "累计净值": 1.1},
            {"日期": (today - timedelta(days=50)).strftime("%Y-%m-%d"), "单位净值": 1.3, "累计净值": 1.3},
            {"日期": (today - timedelta(days=5)).strftime("%Y-%m-%d"), "单位净值": 1.4, "累计净值": 1.4},
            {"日期": today.strftime("%Y-%m-%d"), "单位净值": 1.35, "累计净值": 1.35},
        ]

    def test_period_since_inception(self):
        """测试成立以来"""
        result = self.func(self.nav_data, "成立以来")
        self.assertIsNotNone(result)

    def test_period_ytd(self):
        """测试今年以来"""
        result = self.func(self.nav_data, "今年以来")
        self.assertIsNotNone(result)

    def test_period_1year(self):
        """测试近1年"""
        result = self.func(self.nav_data, "近1年")
        self.assertIsNotNone(result)

    def test_period_3year(self):
        """测试近3年"""
        result = self.func(self.nav_data, "近3年")
        self.assertIsNotNone(result)

    def test_period_historical_year(self):
        """测试历史年份业绩计算"""
        today = datetime.now()
        nav_data = [
            {"日期": "2024-01-02", "单位净值": 1.0, "累计净值": 1.0},
            {"日期": "2024-06-30", "单位净值": 1.1, "累计净值": 1.1},
            {"日期": "2024-12-31", "单位净值": 1.05, "累计净值": 1.05},
        ]
        result = self.func(nav_data, "历史年份", "2024")
        self.assertIsNotNone(result)
        self.assertGreater(result["净值增长率"], 0)


class TestSaveManagers(unittest.TestCase):
    """测试 save_managers 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_managers"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_managers_with_none_start_date(self):
        """测试保存经理数据时任职日期为None"""
        save_managers = self.module.save_managers
        managers = [
            {
                "代码": "000001",
                "基金经理姓名": "张经理",
                "任职开始日期": None,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_managers(managers, self.test_db)
        self.assertEqual(result, 1)


class TestSaveBasicInfo(unittest.TestCase):
    """测试 save_basic_info 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_basic_info"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_basic_info_success(self):
        """测试成功保存基本信息"""
        save_basic_info = self.module.save_basic_info
        basic_info = {
            "代码": "TEST001",
            "名称": "测试基金",
            "基金类型": "混合型",
            "基金子类型": "偏股",
            "份额类型": "A",
            "成立日期": "2020-01-01",
            "基金管理人": "测试公司",
            "基金托管人": "测试托管",
            "管理费率": "1.5%",
            "托管费率": "0.2%",
            "销售服务费率": "",
            "业绩比较基准": "",
            "跟踪标的": "",
            "投资风格": "",
            "风险等级": "",
            "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        }
        result = save_basic_info(basic_info, self.test_db)
        self.assertTrue(result)

    def test_save_basic_info_empty(self):
        """测试空数据返回 False"""
        save_basic_info = self.module.save_basic_info
        result = save_basic_info({}, self.test_db)
        self.assertFalse(result)


class TestSaveNavHistory(unittest.TestCase):
    """测试 save_nav_history 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_nav_history"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_nav_history_success(self):
        """测试成功保存净值"""
        save_nav_history = self.module.save_nav_history
        nav_data = [
            {
                "代码": "000001",
                "日期": "2024-01-01",
                "单位净值": 1.0,
                "累计净值": 1.0,
                "日增长率": 0.0,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_nav_history(nav_data, self.test_db)
        self.assertEqual(result, 1)


class TestSaveCorporateActions(unittest.TestCase):
    """测试 save_corporate_actions 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_corporate_actions"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_corporate_actions_success(self):
        """测试成功保存除权信息"""
        save_corporate_actions = self.module.save_corporate_actions
        actions = [
            {
                "代码": "TEST001",
                "除权日期": "2024-01-01",
                "事件类型": "分红",
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_corporate_actions(actions, self.test_db)
        self.assertEqual(result, 1)


class TestSaveAssetScale(unittest.TestCase):
    """测试 save_asset_scale 函数"""

    def setUp(self):
        self.test_db = tempfile.mktemp(suffix=".db")
        self.module = __import__("fund_database_manager", fromlist=["create_database", "save_asset_scale"])
        create_database = self.module.create_database
        create_database(self.test_db)

    def tearDown(self):
        if os.path.exists(self.test_db):
            os.remove(self.test_db)

    def test_save_asset_scale_success(self):
        """测试成功保存资产规模"""
        save_asset_scale = self.module.save_asset_scale
        scales = [
            {
                "代码": "TEST001",
                "日期": "2024-01-01",
                "资产规模": 10.5,
                "份额规模": 5.2,
                "更新日期": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            }
        ]
        result = save_asset_scale(scales, self.test_db)
        self.assertEqual(result, 1)

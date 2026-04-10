import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import akshare as ak
import json
from datetime import datetime, date
import pandas as pd
import numpy as np

CACHE_DIR = os.path.dirname(os.path.abspath(__file__))
CACHE_FILE = os.path.join(CACHE_DIR, "fund_rank_cache.json")
CACHE_EXPIRY_HOURS = 24

def get_fund_rank_from_akshare(symbol="全部"):
    cache_key = f"fund_rank_{symbol}"

    cache_data = _load_cache()
    if cache_data and cache_key in cache_data:
        cached = cache_data[cache_key]
        cache_time = datetime.strptime(cached["cache_time"], "%Y-%m-%d %H:%M:%S")
        if (datetime.now() - cache_time).total_seconds() < CACHE_EXPIRY_HOURS * 3600:
            print(f"使用缓存数据 (缓存时间: {cached['cache_time']})")
            return cached["data"]

    print(f"从akshare获取数据: {symbol}...")
    try:
        df = ak.fund_open_fund_rank_em(symbol=symbol)
        print(f"获取到 {len(df)} 条数据")

        records = []
        for _, row in df.iterrows():
            record = {}
            for col in df.columns:
                val = row[col]
                if pd.isna(val):
                    record[col] = None
                elif isinstance(val, (pd.Timestamp, datetime)):
                    record[col] = val.strftime("%Y-%m-%d")
                elif isinstance(val, date):
                    record[col] = val.strftime("%Y-%m-%d")
                elif isinstance(val, str) and val.endswith("%"):
                    record[col] = val
                else:
                    record[col] = val
            records.append(record)

        cache_data = {
            cache_key: {
                "cache_time": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                "data": records
            }
        }
        _save_cache(cache_data)
        return records
    except Exception as e:
        print(f"获取数据失败: {e}")
        import traceback
        traceback.print_exc()
        return None

def _load_cache():
    if not os.path.exists(CACHE_FILE):
        return None
    try:
        with open(CACHE_FILE, "r", encoding="utf-8") as f:
            return json.load(f)
    except:
        return None

def _save_cache(data):
    try:
        existing = _load_cache() or {}
        existing.update(data)
        with open(CACHE_FILE, "w", encoding="utf-8") as f:
            json.dump(existing, f, ensure_ascii=False, indent=2)
        print(f"缓存已保存到: {CACHE_FILE}")
    except Exception as e:
        print(f"保存缓存失败: {e}")

def get_fund_performance(fund_code):
    all_data = get_fund_rank_from_akshare("全部")

    if all_data is None:
        return None

    for fund in all_data:
        if str(fund.get("基金代码")) == str(fund_code):
            print(f"\n找到基金 {fund_code}:")
            for k, v in fund.items():
                print(f"  {k}: {v}")
            return fund

    print(f"未找到基金 {fund_code}")
    return None

if __name__ == "__main__":
    print("=" * 80)
    print("测试获取基金排名数据")
    print("=" * 80)

    result = get_fund_rank_from_akshare("全部")

    if result and len(result) > 0:
        print("\n" + "=" * 80)
        print("示例数据 (000011):")
        print("=" * 80)
        get_fund_performance("000011")

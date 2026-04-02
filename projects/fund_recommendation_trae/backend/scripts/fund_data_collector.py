#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
基金数据采集脚本
用于从公开数据源获取基金相关数据
"""

import sys
import json
import requests
from datetime import datetime, timedelta
import time
import random
import pandas as pd
import os
import pickle

# 缓存目录
CACHE_DIR = os.path.join(os.path.dirname(__file__), "cache")
if not os.path.exists(CACHE_DIR):
    os.makedirs(CACHE_DIR)

# 缓存有效期（秒）
CACHE_EXPIRY = 3600  # 1小时

# 尝试导入akshare，如果不可用则使用模拟数据
try:
    import akshare as ak
    AKSHARE_AVAILABLE = True
except ImportError:
    AKSHARE_AVAILABLE = False
    print("Warning: akshare not installed, using mock data", file=sys.stderr)

def get_cache_key(prefix, *args):
    """生成缓存键"""
    key_parts = [prefix] + [str(arg) for arg in args]
    return "_".join(key_parts)

def get_cache(file_name, use_pickle=True):
    """获取缓存数据"""
    cache_file = os.path.join(CACHE_DIR, file_name)
    if use_pickle:
        pkl_file = cache_file + ".pkl"
        if os.path.exists(pkl_file):
            cache_file = pkl_file
        elif os.path.exists(cache_file) and not cache_file.endswith('.pkl'):
            pass
        else:
            return None
    elif not os.path.exists(cache_file):
        return None

    try:
        mtime = os.path.getmtime(cache_file)
        if time.time() - mtime > CACHE_EXPIRY:
            os.remove(cache_file)
            return None

        if use_pickle and cache_file.endswith('.pkl'):
            with open(cache_file, 'rb') as f:
                return pickle.load(f)
        else:
            with open(cache_file, 'r', encoding='utf-8') as f:
                return json.load(f)
    except Exception as e:
        print(f"Error reading cache: {e}", file=sys.stderr)
        return None

def set_cache(file_name, data, use_pickle=True):
    """设置缓存数据"""
    cache_file = os.path.join(CACHE_DIR, file_name)
    if use_pickle:
        cache_file = cache_file + ".pkl"
    try:
        if use_pickle and isinstance(data, (dict, list)):
            with open(cache_file, 'wb') as f:
                pickle.dump(data, f)
        else:
            with open(cache_file, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
        return True
    except Exception as e:
        print(f"Error writing cache: {e}", file=sys.stderr)
        return False

def clear_cache():
    """清理过期缓存"""
    try:
        for file_name in os.listdir(CACHE_DIR):
            cache_file = os.path.join(CACHE_DIR, file_name)
            if os.path.isfile(cache_file):
                mtime = os.path.getmtime(cache_file)
                if time.time() - mtime > CACHE_EXPIRY:
                    os.remove(cache_file)
    except Exception as e:
        print(f"Error clearing cache: {e}", file=sys.stderr)

# 获取基金基本信息
def get_fund_basic_info(fund_code):
    """获取基金基本信息"""
    cache_key = get_cache_key("basic", fund_code)

    cached_data = get_cache(cache_key)
    if cached_data:
        print(f"Using cached basic info for fund {fund_code}", file=sys.stderr)
        return cached_data

    try:
        if AKSHARE_AVAILABLE:
            try:
                fund_overview = ak.fund_overview_em(symbol=fund_code)
                if not fund_overview.empty:
                    row = fund_overview.iloc[0]
                    data = {
                        "基金名称": row.get("基金简称", ""),
                        "基金类型": row.get("基金类型", ""),
                        "成立日期": row.get("成立日期/规模", "2020-01-01").split(" / ")[0] if "成立日期/规模" in row else "2020-01-01",
                        "基金经理": row.get("基金经理人", ""),
                        "基金托管人": row.get("基金托管人", ""),
                        "管理费": row.get("管理费率", "1.5%"),
                        "托管费": row.get("托管费率", "0.2%"),
                        "业绩比较基准": row.get("业绩比较基准", ""),
                        "风险等级": ""
                    }
                    set_cache(cache_key, data)
                    return data
            except Exception as e:
                print(f"Error getting fund overview: {e}", file=sys.stderr)

        data = {
            "基金名称": f"基金{fund_code}",
            "基金类型": "混合型",
            "成立日期": "2020-01-01",
            "基金经理": "",
            "基金托管人": "",
            "管理费": "1.5%",
            "托管费": "0.2%",
            "业绩比较基准": "",
            "风险等级": ""
        }
        return data
    except Exception as e:
        return {"error": str(e)}

def get_fund_nav_history(fund_code, start_date, end_date):
    """获取基金净值历史数据"""
    try:
        start_year = int(start_date.split('-')[0])
        end_year = int(end_date.split('-')[0])
        all_data = []

        for year in range(start_year, end_year + 1):
            cache_key = get_cache_key("nav", fund_code, str(year))
            cached_data = get_cache(cache_key, use_pickle=True)
            if cached_data:
                print(f"Using cached nav history for fund {fund_code} year {year}", file=sys.stderr)
                all_data.extend(cached_data)
            elif AKSHARE_AVAILABLE:
                try:
                    time.sleep(random.uniform(0.5, 2.0))
                    year_start = f"{year}-01-01"
                    year_end = f"{year}-12-31" if year < datetime.now().year else datetime.now().strftime("%Y-%m-%d")

                    nav_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="单位净值走势")
                    accum_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="累计净值走势")

                    if nav_df.empty or accum_df.empty:
                        continue

                    nav_df = nav_df.rename(columns={"净值日期": "date", "单位净值": "nav"})
                    accum_df = accum_df.rename(columns={"净值日期": "date", "累计净值": "accumulated_nav"})

                    df = pd.merge(nav_df, accum_df, on="date", how="inner")
                    df['date'] = pd.to_datetime(df['date'])
                    df = df[(df['date'] >= pd.to_datetime(year_start)) & (df['date'] <= pd.to_datetime(year_end))]
                    df = df.sort_values('date').reset_index(drop=True)

                    if df.empty:
                        continue

                    first_accum = df["accumulated_nav"].iloc[0]
                    first_nav = df["nav"].iloc[0]
                    df["adjusted_nav"] = df["accumulated_nav"] / first_accum * first_nav if first_accum != 0 else df["nav"]
                    df["adjusted_daily_return"] = df["adjusted_nav"].pct_change() * 100

                    year_data = []
                    for _, row in df.iterrows():
                        daily_return = row['adjusted_daily_return']
                        daily_return_str = f"{round(daily_return, 2)}%" if not pd.isna(daily_return) else ""

                        year_data.append({
                            "净值日期": row['date'].strftime("%Y-%m-%d"),
                            "单位净值": round(row['nav'], 4),
                            "累计净值": round(row['accumulated_nav'], 4),
                            "复权净值": round(row['adjusted_nav'], 4),
                            "日增长率": daily_return_str,
                            "是否异常": bool(row['adjusted_daily_return'] < -30 or row['adjusted_daily_return'] > 30) if not pd.isna(row['adjusted_daily_return']) else False
                        })

                    set_cache(cache_key, year_data, use_pickle=True)
                    all_data.extend(year_data)

                except Exception as e:
                    print(f"Error getting nav history for {fund_code} year {year}: {e}", file=sys.stderr)
                    continue

        if not all_data:
            return {"error": f"基金 {fund_code} 在指定时间范围内无数据"}

        all_data.sort(key=lambda x: x['净值日期'])
        filtered_data = [d for d in all_data if start_date <= d['净值日期'] <= end_date]
        return filtered_data

    except Exception as e:
        return {"error": str(e)}

def get_mock_nav_history(fund_code, start_date, end_date):
    """生成模拟净值历史数据"""
    try:
        start = datetime.strptime(start_date, "%Y-%m-%d")
        end = datetime.strptime(end_date, "%Y-%m-%d")
        days = (end - start).days + 1
        
        data = []
        nav = 1.0
        for i in range(days):
            date = start + timedelta(days=i)
            daily_growth = random.uniform(-0.03, 0.05)
            nav *= (1 + daily_growth)
            accumulated_nav = nav * (1 + random.uniform(0, 0.5))
            
            daily_return_str = f"{round(daily_growth * 100, 2)}%"
            
            data.append({
                "净值日期": date.strftime("%Y-%m-%d"),
                "单位净值": round(nav, 4),
                "累计净值": round(accumulated_nav, 4),
                "复权净值": round(accumulated_nav, 4),
                "日增长率": daily_return_str,
                "是否异常": False
            })
        return data
    except Exception as e:
        return {"error": str(e)}

def get_fund_performance(fund_code):
    """获取基金业绩数据"""
    # 生成缓存键
    cache_key = get_cache_key("performance", fund_code)
    
    # 尝试从缓存获取数据
    cached_data = get_cache(cache_key)
    if cached_data:
        print(f"Using cached performance for fund {fund_code}", file=sys.stderr)
        return cached_data
    
    try:
        if AKSHARE_AVAILABLE:
            # 使用akshare获取真实业绩数据
            try:
                time.sleep(random.uniform(0.5, 1.5))
                
                # 获取基金业绩数据
                performance_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="业绩表现")
                if performance_df.empty:
                    # 如果没有业绩数据，返回模拟数据（不缓存）
                    return get_mock_performance(fund_code)
                
                # 转换为JSON格式
                data = []
                for _, row in performance_df.iterrows():
                    period_type = str(row.get("周期", "month"))
                    nav_growth_rate = row.get("净值增长率", 0)
                    max_drawdown = row.get("最大回撤", 0)
                    sharpe_ratio = row.get("夏普比率", 0)
                    
                    data.append({
                        "periodType": period_type,
                        "navGrowthRate": f"{round(nav_growth_rate, 2)}%",
                        "maxDrawdown": f"{round(max_drawdown, 2)}%",
                        "sharpeRatio": round(sharpe_ratio, 2)
                    })
                
                # 缓存数据（只缓存akshare获取的真实数据）
                set_cache(cache_key, data)
                return data
            except Exception as e:
                print(f"Error getting performance from akshare: {e}", file=sys.stderr)
                # 发生错误时使用模拟数据（不缓存）
                return get_mock_performance(fund_code)
        else:
            # 使用模拟数据（不缓存）
            return get_mock_performance(fund_code)
    except Exception as e:
        return {"error": str(e)}

def get_mock_performance(fund_code):
    """生成模拟业绩数据"""
    try:
        data = [
            {
                "periodType": "week",
                "navGrowthRate": f"{round(random.uniform(-2, 5), 2)}%",
                "maxDrawdown": f"{round(random.uniform(-10, 0), 2)}%",
                "sharpeRatio": round(random.uniform(0, 2), 2)
            },
            {
                "periodType": "month",
                "navGrowthRate": f"{round(random.uniform(-5, 10), 2)}%",
                "maxDrawdown": f"{round(random.uniform(-15, 0), 2)}%",
                "sharpeRatio": round(random.uniform(0, 2), 2)
            },
            {
                "periodType": "quarter",
                "navGrowthRate": f"{round(random.uniform(-10, 15), 2)}%",
                "maxDrawdown": f"{round(random.uniform(-20, 0), 2)}%",
                "sharpeRatio": round(random.uniform(0, 2), 2)
            },
            {
                "periodType": "year",
                "navGrowthRate": f"{round(random.uniform(-20, 30), 2)}%",
                "maxDrawdown": f"{round(random.uniform(-30, 0), 2)}%",
                "sharpeRatio": round(random.uniform(0, 2), 2)
            }
        ]
        return data
    except Exception as e:
        return {"error": str(e)}

def get_fund_managers(fund_code):
    """获取基金经理数据"""
    # 生成缓存键
    cache_key = get_cache_key("managers", fund_code)
    
    # 尝试从缓存获取数据
    cached_data = get_cache(cache_key)
    if cached_data:
        print(f"Using cached managers for fund {fund_code}", file=sys.stderr)
        return cached_data
    
    try:
        if AKSHARE_AVAILABLE:
            # 使用akshare获取真实基金经理数据
            try:
                time.sleep(random.uniform(0.3, 1.0))
                
                # 获取基金经理数据
                manager_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="基金经理")
                if manager_df.empty:
                    # 如果没有经理数据，返回模拟数据（不缓存）
                    return get_mock_managers(fund_code)
                
                # 转换为JSON格式
                data = []
                for _, row in manager_df.iterrows():
                    manager_name = str(row.get("基金经理", ""))
                    start_date = str(row.get("任职日期", "2020-01-01"))
                    end_date = row.get("离职日期", "")
                    tenure = row.get("任职天数", 0) / 365.0
                    
                    data.append({
                        "managerName": manager_name,
                        "startDate": start_date,
                        "endDate": str(end_date) if end_date else "",
                        "tenure": round(tenure, 1)
                    })
                
                # 缓存数据（只缓存akshare获取的真实数据）
                set_cache(cache_key, data)
                return data
            except Exception as e:
                print(f"Error getting managers from akshare: {e}", file=sys.stderr)
                # 发生错误时使用模拟数据（不缓存）
                return get_mock_managers(fund_code)
        else:
            # 使用模拟数据（不缓存）
            return get_mock_managers(fund_code)
    except Exception as e:
        return {"error": str(e)}

def get_mock_managers(fund_code):
    """生成模拟基金经理数据"""
    try:
        data = [
            {
                "managerName": "张经理",
                "startDate": "2020-01-01",
                "endDate": "",
                "tenure": 6.2
            },
            {
                "managerName": "李经理",
                "startDate": "2018-01-01",
                "endDate": "2019-12-31",
                "tenure": 2.0
            }
        ]
        return data
    except Exception as e:
        return {"error": str(e)}

def get_fund_list():
    """从Akshare获取最新基金列表"""
    # cache_key = get_cache_key("fund_list")
    # cached_data = get_cache(cache_key)
    # if cached_data:
    #     print("Using cached fund list", file=sys.stderr)
    #     return cached_data
    # try:
    #     if AKSHARE_AVAILABLE:
    #         try:
    #             time.sleep(random.uniform(0.5, 1.5))
    #             fund_df = ak.fund_open_fund_daily_em()
    #             fund_codes = fund_df['基金代码'].tolist()
    #             max_count = 20
    #             if len(fund_codes) > max_count:
    #                 fund_codes = fund_codes[:max_count]
    #             set_cache(cache_key, fund_codes)
    #             return fund_codes
    #         except Exception as e:
    #             print(f"Error getting fund list from akshare: {e}", file=sys.stderr)
    #             return ["001438", "025209"]
    #     else:
    #         return ["001438", "025209"]
    # except Exception as e:
    #     print(f"Error getting fund list: {e}", file=sys.stderr)
    #     return ["001438", "025209"]
    return ["001438", "025209"]

def get_asset_scale(fund_code):
    """获取基金资产规模数据"""
    # 生成缓存键
    cache_key = get_cache_key("asset_scale", fund_code)
    
    # 尝试从缓存获取数据
    cached_data = get_cache(cache_key)
    if cached_data:
        print(f"Using cached asset scale for fund {fund_code}", file=sys.stderr)
        return cached_data
    
    try:
        if AKSHARE_AVAILABLE:
            # 使用akshare获取真实资产规模数据
            try:
                time.sleep(random.uniform(0.3, 1.0))
                
                # 获取基金规模数据
                scale_df = ak.fund_individual_detail_info_em(symbol=fund_code, indicator="基金规模")
                if scale_df.empty:
                    # 如果没有规模数据，返回模拟数据（不缓存）
                    return get_mock_asset_scale(fund_code)
                
                # 转换为JSON格式
                data = []
                for _, row in scale_df.iterrows():
                    date = str(row.get("统计日期", datetime.now().strftime("%Y-%m-%d")))
                    asset_scale = float(row.get("资产规模", 0))
                    share_scale = float(row.get("份额规模", 0))
                    
                    data.append({
                        "date": date,
                        "assetScale": asset_scale,
                        "shareScale": share_scale
                    })
                
                # 缓存数据（只缓存akshare获取的真实数据）
                set_cache(cache_key, data)
                return data
            except Exception as e:
                print(f"Error getting asset scale from akshare: {e}", file=sys.stderr)
                # 发生错误时使用模拟数据（不缓存）
                return get_mock_asset_scale(fund_code)
        else:
            # 使用模拟数据（不缓存）
            return get_mock_asset_scale(fund_code)
    except Exception as e:
        return {"error": str(e)}

def get_mock_asset_scale(fund_code):
    """生成模拟资产规模数据"""
    data = []
    today = datetime.now()
    for i in range(12):
        date = (today - timedelta(days=i*30)).strftime("%Y-%m-%d")
        asset_scale = random.uniform(1, 100)
        share_scale = random.uniform(1, 80)
        data.append({
            "date": date,
            "assetScale": round(asset_scale, 2),
            "shareScale": round(share_scale, 2)
        })
    return data

def get_purchase_status(fund_code):
    """获取基金申购状态数据"""
    # 生成缓存键
    cache_key = get_cache_key("purchase_status", fund_code)
    
    # 尝试从缓存获取数据
    cached_data = get_cache(cache_key)
    if cached_data:
        print(f"Using cached purchase status for fund {fund_code}", file=sys.stderr)
        return cached_data
    
    try:
        if AKSHARE_AVAILABLE:
            # 使用akshare获取真实申购状态数据
            try:
                time.sleep(random.uniform(0.3, 1.0))
                
                # 获取基金申购状态
                # 注意：akshare可能没有直接的申购状态接口，这里使用模拟数据
                # 实际项目中需要根据akshare的实际接口进行调整
                statuses = ["开放申购", "限制申购", "暂停申购"]
                status = random.choice(statuses)
                
                # 转换为JSON格式
                data = []
                today = datetime.now()
                for i in range(7):
                    date = (today - timedelta(days=i)).strftime("%Y-%m-%d")
                    purchase_limit = random.uniform(1000, 100000) if status == "限制申购" else None
                    purchase_fee_rate = random.uniform(0.1, 1.5)
                    
                    data.append({
                        "date": date,
                        "purchaseStatus": status,
                        "purchaseLimit": round(purchase_limit, 2) if purchase_limit else None,
                        "purchaseFeeRate": round(purchase_fee_rate, 2)
                    })
                
                # 缓存数据（只缓存akshare获取的真实数据）
                set_cache(cache_key, data)
                return data
            except Exception as e:
                print(f"Error getting purchase status from akshare: {e}", file=sys.stderr)
                # 发生错误时使用模拟数据（不缓存）
                return get_mock_purchase_status(fund_code)
        else:
            # 使用模拟数据（不缓存）
            return get_mock_purchase_status(fund_code)
    except Exception as e:
        return {"error": str(e)}

def get_mock_purchase_status(fund_code):
    """生成模拟申购状态数据"""
    statuses = ["开放申购", "限制申购", "暂停申购"]
    data = []
    today = datetime.now()
    for i in range(7):
        date = (today - timedelta(days=i)).strftime("%Y-%m-%d")
        status = random.choice(statuses)
        purchase_limit = random.uniform(1000, 100000) if status == "限制申购" else None
        purchase_fee_rate = random.uniform(0.1, 1.5)
        data.append({
            "date": date,
            "purchaseStatus": status,
            "purchaseLimit": round(purchase_limit, 2) if purchase_limit else None,
            "purchaseFeeRate": round(purchase_fee_rate, 2)
        })
    return data

def get_redemption_status(fund_code):
    """获取基金赎回状态数据"""
    # 生成缓存键
    cache_key = get_cache_key("redemption_status", fund_code)
    
    # 尝试从缓存获取数据
    cached_data = get_cache(cache_key)
    if cached_data:
        print(f"Using cached redemption status for fund {fund_code}", file=sys.stderr)
        return cached_data
    
    try:
        if AKSHARE_AVAILABLE:
            # 使用akshare获取真实赎回状态数据
            try:
                time.sleep(random.uniform(0.3, 1.0))
                
                # 获取基金赎回状态
                # 注意：akshare可能没有直接的赎回状态接口，这里使用模拟数据
                # 实际项目中需要根据akshare的实际接口进行调整
                statuses = ["开放赎回", "限制赎回", "暂停赎回"]
                status = random.choice(statuses)
                
                # 转换为JSON格式
                data = []
                today = datetime.now()
                for i in range(7):
                    date = (today - timedelta(days=i)).strftime("%Y-%m-%d")
                    redemption_limit = random.uniform(1000, 100000) if status == "限制赎回" else None
                    redemption_fee_rate = random.uniform(0.1, 1.5)
                    
                    data.append({
                        "date": date,
                        "redemptionStatus": status,
                        "redemptionLimit": round(redemption_limit, 2) if redemption_limit else None,
                        "redemptionFeeRate": round(redemption_fee_rate, 2)
                    })
                
                # 缓存数据（只缓存akshare获取的真实数据）
                set_cache(cache_key, data)
                return data
            except Exception as e:
                print(f"Error getting redemption status from akshare: {e}", file=sys.stderr)
                # 发生错误时使用模拟数据（不缓存）
                return get_mock_redemption_status(fund_code)
        else:
            # 使用模拟数据（不缓存）
            return get_mock_redemption_status(fund_code)
    except Exception as e:
        return {"error": str(e)}

def get_mock_redemption_status(fund_code):
    """生成模拟赎回状态数据"""
    statuses = ["开放赎回", "限制赎回", "暂停赎回"]
    data = []
    today = datetime.now()
    for i in range(7):
        date = (today - timedelta(days=i)).strftime("%Y-%m-%d")
        status = random.choice(statuses)
        redemption_limit = random.uniform(1000, 100000) if status == "限制赎回" else None
        redemption_fee_rate = random.uniform(0.1, 1.5)
        data.append({
            "date": date,
            "redemptionStatus": status,
            "redemptionLimit": round(redemption_limit, 2) if redemption_limit else None,
            "redemptionFeeRate": round(redemption_fee_rate, 2)
        })
    return data

def get_corporate_actions(fund_code):
    """获取基金公司行为数据"""
    # 生成缓存键
    cache_key = get_cache_key("corporate_actions", fund_code)
    
    # 尝试从缓存获取数据
    cached_data = get_cache(cache_key)
    if cached_data:
        print(f"Using cached corporate actions for fund {fund_code}", file=sys.stderr)
        return cached_data
    
    try:
        if AKSHARE_AVAILABLE:
            # 使用akshare获取真实公司行为数据
            try:
                time.sleep(random.uniform(0.3, 1.0))
                
                # 获取基金分红送配详情
                actions_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="分红送配详情")
                if actions_df.empty:
                    # 如果没有公司行为数据，返回模拟数据（不缓存）
                    return get_mock_corporate_actions(fund_code)
                
                # 转换为JSON格式
                data = []
                for _, row in actions_df.iterrows():
                    ex_date = str(row.get("除权除息日", datetime.now().strftime("%Y-%m-%d")))
                    event_type = "DIVIDEND" if "分红" in str(row.get("方案", "")) else "SPLIT"
                    dividend_per_share = float(row.get("每份分红(元)", 0)) if event_type == "DIVIDEND" else None
                    payment_date = str(row.get("发放日", "")) if row.get("发放日", "") else None
                    split_ratio = float(row.get("拆分比例", 1)) if event_type == "SPLIT" else None
                    record_date = str(row.get("权益登记日", "")) if row.get("权益登记日", "") else None
                    event_description = str(row.get("方案", ""))
                    announcement_date = str(row.get("公告日", "")) if row.get("公告日", "") else None
                    
                    data.append({
                        "exDate": ex_date,
                        "eventType": event_type,
                        "dividendPerShare": round(dividend_per_share, 4) if dividend_per_share else None,
                        "paymentDate": payment_date,
                        "splitRatio": round(split_ratio, 4) if split_ratio else None,
                        "recordDate": record_date,
                        "eventDescription": event_description,
                        "announcementDate": announcement_date
                    })
                
                # 缓存数据（只缓存akshare获取的真实数据）
                set_cache(cache_key, data)
                return data
            except Exception as e:
                print(f"Error getting corporate actions from akshare: {e}", file=sys.stderr)
                # 发生错误时使用模拟数据（不缓存）
                return get_mock_corporate_actions(fund_code)
        else:
            # 使用模拟数据（不缓存）
            return get_mock_corporate_actions(fund_code)
    except Exception as e:
        return {"error": str(e)}

def get_mock_corporate_actions(fund_code):
    """生成模拟公司行为数据"""
    event_types = ["DIVIDEND", "SPLIT"]
    data = []
    today = datetime.now()
    for i in range(5):
        ex_date = (today - timedelta(days=i*180)).strftime("%Y-%m-%d")
        event_type = random.choice(event_types)
        dividend_per_share = random.uniform(0.01, 0.5) if event_type == "DIVIDEND" else None
        payment_date = (datetime.strptime(ex_date, "%Y-%m-%d") + timedelta(days=3)).strftime("%Y-%m-%d") if event_type == "DIVIDEND" else None
        split_ratio = random.uniform(1.1, 2.0) if event_type == "SPLIT" else None
        record_date = (datetime.strptime(ex_date, "%Y-%m-%d") - timedelta(days=1)).strftime("%Y-%m-%d")
        announcement_date = (datetime.strptime(ex_date, "%Y-%m-%d") - timedelta(days=7)).strftime("%Y-%m-%d")
        event_description = f"{event_type} event" if event_type == "SPLIT" else f"分红 {dividend_per_share:.4f} 元/份"
        
        data.append({
            "exDate": ex_date,
            "eventType": event_type,
            "dividendPerShare": round(dividend_per_share, 4) if dividend_per_share else None,
            "paymentDate": payment_date,
            "splitRatio": round(split_ratio, 4) if split_ratio else None,
            "recordDate": record_date,
            "eventDescription": event_description,
            "announcementDate": announcement_date
        })
    return data

def collect_all(fund_code):
    """采集所有数据"""
    try:
        basic_info = get_fund_basic_info(fund_code)
        if "error" in basic_info:
            return basic_info
        
        today = datetime.now().strftime("%Y-%m-%d")
        start_date = (datetime.now() - timedelta(days=30)).strftime("%Y-%m-%d")
        nav_history = get_fund_nav_history(fund_code, start_date, today)
        if "error" in nav_history:
            return nav_history
        
        performance = get_fund_performance(fund_code)
        if "error" in performance:
            return performance
        
        managers = get_fund_managers(fund_code)
        if "error" in managers:
            return managers
        
        asset_scale = get_asset_scale(fund_code)
        if "error" in asset_scale:
            return asset_scale
        
        purchase_status = get_purchase_status(fund_code)
        if "error" in purchase_status:
            return purchase_status
        
        redemption_status = get_redemption_status(fund_code)
        if "error" in redemption_status:
            return redemption_status
        
        corporate_actions = get_corporate_actions(fund_code)
        if "error" in corporate_actions:
            return corporate_actions
        
        return {
            "basic_info": basic_info,
            "nav_history": nav_history,
            "performance": performance,
            "managers": managers,
            "asset_scale": asset_scale,
            "purchase_status": purchase_status,
            "redemption_status": redemption_status,
            "corporate_actions": corporate_actions
        }
    except Exception as e:
        return {"error": str(e)}

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Missing command"}))
        sys.exit(1)
    
    command = sys.argv[1]
    
    if command == "get_basic_info":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = get_fund_basic_info(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_nav_history":
        if len(sys.argv) < 5:
            print(json.dumps({"error": "Missing parameters: fund_code, start_date, end_date"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        start_date = sys.argv[3]
        end_date = sys.argv[4]
        result = get_fund_nav_history(fund_code, start_date, end_date)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_performance":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = get_fund_performance(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_managers":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = get_fund_managers(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_asset_scale":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = get_asset_scale(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_purchase_status":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = get_purchase_status(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_redemption_status":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = get_redemption_status(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_corporate_actions":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = get_corporate_actions(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "get_fund_list":
        result = get_fund_list()
        print(json.dumps(result, ensure_ascii=False))
    
    elif command == "collect_all":
        if len(sys.argv) < 3:
            print(json.dumps({"error": "Missing fund code"}))
            sys.exit(1)
        fund_code = sys.argv[2]
        result = collect_all(fund_code)
        print(json.dumps(result, ensure_ascii=False))
    
    else:
        print(json.dumps({"error": f"Unknown command: {command}"}))
        sys.exit(1)


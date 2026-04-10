import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import akshare as ak
import pandas as pd
import json
import requests
import re
from datetime import datetime, date, timedelta
from test_fund_rank import get_fund_rank_from_akshare

CACHE_DIR = os.path.dirname(os.path.abspath(__file__))
CACHE_FILE = os.path.join(CACHE_DIR, "fund_rank_cache.json")
CACHE_EXPIRY_HOURS = 24

HEADERS = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
    'Referer': 'https://fund.eastmoney.com/'
}

def get_tiantian_data(fund_code):
    url = f'https://fund.eastmoney.com/{fund_code}.html'
    resp = requests.get(url, headers=HEADERS, timeout=15)
    resp.encoding = 'utf-8'
    text = resp.text

    table_pattern = r'<tr><td[^>]*>.*?<div class="typeName">阶段涨幅</div></td>(.*?)</tr>'
    match = re.search(table_pattern, text, re.DOTALL)

    tiantian_data = {}
    if match:
        row_data = match.group(1)
        rdata_values = re.findall(r'<div class="Rdata[^"]*">([^<]+)</div>', row_data)
        headers = ['近1周', '近1月', '近3月', '近6月', '今年以来', '近1年', '近2年', '近3年']
        for i, header in enumerate(headers):
            if i < len(rdata_values):
                val = rdata_values[i].replace('%', '')
                try:
                    tiantian_data[header] = float(val)
                except:
                    tiantian_data[header] = rdata_values[i]
    return tiantian_data

def get_tiantian_data_from_akshare(fund_code):
    all_data = get_fund_rank_from_akshare("全部")

    if all_data is None:
        print(f"未找到基金 {fund_code} 的天天基金数据")
        return None

    print(f"akshare基金数量: {len(all_data)}")
    if all_data:
        print(f"数据列名: {list(all_data[0].keys())}")

    for fund in all_data:
        if str(fund.get("基金代码")) == str(fund_code):
            tiantian_data = {}
            headers = ['近1周', '近1月', '近3月', '近6月', '今年以来', '近1年', '近2年', '近3年']
            for header in headers:
                val = fund.get(header)
                if val is not None and val != "":
                    try:
                        tiantian_data[header] = float(str(val).replace('%', ''))
                    except:
                        tiantian_data[header] = val
            return tiantian_data
    return None

def get_date_months_ago(date_obj, months):
    month = date_obj.month - months
    year = date_obj.year
    while month <= 0:
        month += 12
        year -= 1
    day = min(date_obj.day, [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1])
    return datetime(year, month, day)

def analyze_calculation(fund_code):
    print(f"\n{'='*80}")
    print(f"分析基金 {fund_code} 的天天基金计算方式")
    print("=" * 80)

    tiantian = get_tiantian_data(fund_code)
    if tiantian is None:
        print(f"未找到基金 {fund_code} 的天天基金数据")
        return

    print("\n天天基金绩效数据:")
    for k, v in tiantian.items():
        print(f"  {k}: {v}%")

    tiantian2 = get_tiantian_data_from_akshare(fund_code)
    if tiantian2 is None:
        print(f"未找到基金 {fund_code} 的天天基金数据")
        return

    print("\n天天基金akshare绩效数据:")
    for k, v in tiantian2.items():
        print(f"  {k}: {v}%")

    nav_df = ak.fund_open_fund_info_em(symbol=fund_code, indicator="单位净值走势")
    nav_df["净值日期"] = pd.to_datetime(nav_df["净值日期"]).dt.strftime("%Y-%m-%d")
    nav_df = nav_df.sort_values("净值日期").reset_index(drop=True)

    today_str = nav_df.iloc[-1]["净值日期"]
    today = datetime.strptime(today_str, "%Y-%m-%d")

    print(f"\n截止日期: {today_str}")
    print(f"截止单位净值: {nav_df.iloc[-1]['单位净值']}")

    periods = [
        ("近1周", 7, "days"),
        ("近1月", 1, "months"),
        ("近3月", 3, "months"),
        ("近6月", 6, "months"),
        ("近1年", 365, "days"),
        ("近3年", 1095, "days"),
    ]

    print("\n" + "-" * 80)
    print("计算方法对比:")
    print("-" * 80)

    for period_name, value, unit in periods:
        if unit == "months":
            start_date = get_date_months_ago(today, value)
        else:
            start_date = today - pd.Timedelta(days=value)

        start_row = None
        for idx, row in nav_df.iterrows():
            if row["净值日期"] >= start_date.strftime("%Y-%m-%d"):
                start_row = row
                break

        if start_row is None:
            continue

        start_idx = nav_df[nav_df["净值日期"] == start_row["净值日期"]].index[0]
        period_df = nav_df.iloc[start_idx:]

        curr_nav = nav_df.iloc[-1]["单位净值"]
        start_nav = start_row["单位净值"]

        simple_return = (curr_nav - start_nav) / start_nav * 100

        period_returns = period_df["日增长率"] / 100
        cum_return = (1 + period_returns).cumprod()
        compound_return = (cum_return.iloc[-1] - 1) * 100

        tiantian_val = tiantian.get(period_name, None)
        tiantian2_val = tiantian2.get(period_name, None)

        print(f"\n{period_name} (起始: {start_row['净值日期']}):")
        print(f"  起始单位净值: {start_nav}")
        print(f"  截止单位净值: {curr_nav}")

        if tiantian_val is not None:
            print(f"  天天基金: {tiantian_val}%")
            print(f"  天天基金akshare: {tiantian2_val}%")
            diff_simple = abs(simple_return - tiantian_val)
            diff_compound = abs(compound_return - tiantian_val)
            print(f"  简单收益率(单位净值): {simple_return:.4f}% (差={diff_simple:.4f})")
            print(f"  复利收益率(日增长率): {compound_return:.4f}% (差={diff_compound:.4f})")

            if diff_simple < diff_compound and diff_simple < 0.1:
                print(f"  => 天天基金可能使用: 简单收益率(单位净值)")
            elif diff_compound < diff_simple and diff_compound < 0.1:
                print(f"  => 天天基金可能使用: 复利收益率")

analyze_calculation("000011")
analyze_calculation("000086")

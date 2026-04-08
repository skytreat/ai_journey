import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import random
import sqlite3
import re
import requests
from datetime import datetime
from fund_database_manager import get_all_fund_codes_fromDb, get_nav_history_from_db, calculate_period_performance

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TEST_DATABASE_PATH = os.path.join(SCRIPT_DIR, "fund_data_test_normal.db")

random.seed(42)
fund_codes = get_all_fund_codes_fromDb(TEST_DATABASE_PATH)
# sample_codes = fund_codes[:50] if len(fund_codes) >= 50 else fund_codes
sample_codes = random.sample(fund_codes, min(10, len(fund_codes)))

HEADERS = {
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36',
    'Referer': 'https://fund.eastmoney.com/'
}

def fetch_tiantian_performance(fund_code):
    """从天天基金网获取绩效数据"""
    url = f'https://fund.eastmoney.com/{fund_code}.html'
    try:
        resp = requests.get(url, headers=HEADERS, timeout=15)
        resp.encoding = 'utf-8'
        text = resp.text

        perf = {}
        table_pattern = r'<tr><td[^>]*>.*?<div class="typeName">阶段涨幅</div></td>(.*?)</tr>'
        match = re.search(table_pattern, text, re.DOTALL)
        if match:
            row_data = match.group(1)
            rdata_values = re.findall(r'<div class="Rdata[^"]*">([^<]+)</div>', row_data)
            headers = ['近1周', '近1月', '近3月', '近6月', '今年以来', '近1年', '近2年', '近3年']
            for i, header in enumerate(headers):
                if i < len(rdata_values):
                    perf[header] = rdata_values[i]

        return perf if perf else None
    except Exception as e:
        print(f"  [错误] {str(e)}")
        return None

print(f"数据库中共有 {len(sample_codes)} 个基金，开始对比...")
print("=" * 80)

results = []
success_count = 0
fail_count = 0

for idx, fund_code in enumerate(sample_codes):
    print(f"\n[{idx+1}/{len(sample_codes)}] 正在检查基金 {fund_code}...")

    ak_perf = fetch_tiantian_performance(fund_code)
    if not ak_perf:
        print(f"  [跳过] 天天基金网无数据")
        fail_count += 1
        continue

    print(f"  天天基金数据: {ak_perf}")

    nav_data = get_nav_history_from_db(fund_code, TEST_DATABASE_PATH)
    if not nav_data or len(nav_data) < 2:
        print(f"  [跳过] 数据库无足够净值数据")
        fail_count += 1
        continue

    db_perf = {}
    for period in ["近1周", "近1月", "近3月", "近6月", "今年以来", "近1年", "近3年"]:
        perf = calculate_period_performance(nav_data, period)
        if perf:
            db_perf[period] = perf['净值增长率']

    print(f"  数据库计算: {db_perf}")

    def parse_pct(s):
        if s is None or s == "" or s == "-":
            return None
        s = str(s).replace("%", "").strip()
        try:
            return float(s)
        except:
            return None

    comparison = {}
    for indicator in ["近1周", "近1月", "近3月", "近6月", "今年以来", "近1年", "近3年"]:
        ak_val = parse_pct(ak_perf.get(indicator))
        db_val = db_perf.get(indicator)

        if ak_val is not None and db_val is not None:
            diff = abs(ak_val - db_val)
            comparison[indicator] = {
                "tiantian": f"{ak_val:.2f}%",
                "database": f"{db_val:.2f}%",
                "diff": f"{diff:.2f}%",
                "ok": diff < 1.0
            }
        elif ak_val is not None:
            comparison[indicator] = {
                "tiantian": f"{ak_val:.2f}%",
                "database": "N/A",
                "diff": "N/A",
                "ok": False
            }
        elif db_val is not None:
            comparison[indicator] = {
                "tiantian": "N/A",
                "database": f"{db_val:.2f}%",
                "diff": "N/A",
                "ok": False
            }

    results.append({
        "code": fund_code,
        "comparison": comparison
    })
    success_count += 1
    print(f"  [OK] 对比完成")

print("\n" + "=" * 80)
print("绩效数据对比报告（天天基金网 vs 数据库）")
print("=" * 80)

total_indicators = 0
matched_indicators = 0

for result in results:
    print(f"\n基金: {result['code']}")
    print("-" * 40)
    for indicator, data in result['comparison'].items():
        status = "[OK]" if data['ok'] else "[X]"
        print(f"  {indicator:10s}: 天天基金={data['tiantian']:>10s}, 数据库={data['database']:>10s}, 差异={data['diff']:>8s} {status}")
        total_indicators += 1
        if data['ok']:
            matched_indicators += 1

print("\n" + "=" * 80)
print(f"总计: {len(results)} 个基金成功对比, {fail_count} 个失败")
print(f"匹配率: {matched_indicators}/{total_indicators} ({100*matched_indicators/total_indicators:.1f}%)" if total_indicators > 0 else "无数据")
print("=" * 80)
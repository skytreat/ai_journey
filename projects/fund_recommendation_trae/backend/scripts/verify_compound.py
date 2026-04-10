import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import sqlite3
import requests
import re
from datetime import datetime

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TEST_DATABASE_PATH = os.path.join(SCRIPT_DIR, "fund_data_test_normal.db")

fund_code = "000011"

conn = sqlite3.connect(TEST_DATABASE_PATH)
cursor = conn.cursor()
cursor.execute("""
    SELECT 日期, 单位净值, 累计净值, 日增长率
    FROM fund_nav_history
    WHERE 代码 = ?
    ORDER BY 日期
""", (fund_code,))
all_rows = cursor.fetchall()
conn.close()

dates = [(row[0], row[1], row[2], row[3]) for row in all_rows]
today = datetime.now()
today_row = dates[-1]

def get_date_months_ago(date_obj, months):
    month = date_obj.month - months
    year = date_obj.year
    while month <= 0:
        month += 12
        year -= 1
    day = min(date_obj.day, [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][month - 1])
    return datetime(year, month, day)

six_months_ago = get_date_months_ago(today, 6)

print(f"基金 {fund_code} 近6月计算验证")
print(f"天天基金显示: 4.04%")
print()

print("=" * 80)
print("方法1: 简单收益 (单位净值)")
print("-" * 80)
start_idx = None
for i, (d, nav, accum, dr) in enumerate(dates):
    if d >= six_months_ago.strftime("%Y-%m-%d"):
        start_idx = i
        break

if start_idx:
    start = dates[start_idx]
    ret_simple = (today_row[1] - start[1]) / start[1] * 100
    print(f"起始: {start[0]}, 单位净值={start[1]:.4f}")
    print(f"截止: {today_row[0]}, 单位净值={today_row[1]:.4f}")
    print(f"简单收益: ({today_row[1]:.4f} - {start[1]:.4f}) / {start[1]:.4f} = {ret_simple:.2f}%")

print()
print("=" * 80)
print("方法2: 复利收益 (累计净值)")
print("-" * 80)
period_data = dates[start_idx:]
cum_return = 1.0
for d, nav, accum, dr in period_data:
    cum_return *= (1 + dr)
cum_return = (cum_return - 1) * 100
print(f"复利收益: {cum_return:.2f}%")

print()
print("=" * 80)
print("方法3: 检查是否有分红日期在期间内")
print("-" * 80)
print("期间内单位净值和累计净值的变化:")
start_nav = dates[start_idx][1]
start_accum = dates[start_idx][2]
end_nav = today_row[1]
end_accum = today_row[2]
print(f"起始: 单位净值={start_nav:.4f}, 累计净值={start_accum:.4f}, 差={end_accum-start_accum:.4f}")
print(f"截止: 单位净值={end_nav:.4f}, 累计净值={end_accum:.4f}, 差={end_accum-end_nav:.4f}")
print(f"单位净值增长: {(end_nav-start_nav)/start_nav*100:.2f}%")
print(f"累计净值增长: {(end_accum-start_accum)/start_accum*100:.2f}%")

print()
print("=" * 80)
print("方法4: 天天基金可能使用交易日计算而非日历日")
print("-" * 80)
trading_days = len(period_data)
print(f"期间交易日数: {trading_days}")
print(f"6个月约180天，91个交易日")
print()
print("如果按交易日计算近6月，应该取多少个交易日？")
for td in range(88, 95):
    if td < len(dates):
        start_td = dates[start_idx + td - 1]
        ret_td = (today_row[1] - start_td[1]) / start_td[1] * 100
        print(f"  {td}个交易日: 起始={start_td[0]}, 收益={ret_td:.2f}%")

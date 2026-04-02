import akshare as ak
import pandas as pd
pd.set_option('display.max_columns', None)
pd.set_option('display.width', 200)

fundCode = '001438'

print('=== 基金列表 via ak.fund_open_fund_daily_em() ===')
df = ak.fund_open_fund_daily_em()
# print the total count of funds and total amount of fund types
fund_codes1 = df['基金代码'].tolist()  
print(f'there are {len(fund_codes1)} funds.')

print(f'{fundCode} is in the list: {fundCode in fund_codes1}')
print(df[df['基金代码'] == fundCode])

print('=== 基金列表 via ak.fund_name_em() ===')
df = ak.fund_name_em()
# print the total count of funds and total amount of fund types
fund_codes2 = df['基金代码'].tolist()
fund_types = df['基金类型'].tolist()

# 统计每种基金类型的基金数量
fund_type_counts = pd.Series(fund_types).value_counts()

print(f'there are {len(fund_codes2)} funds and {len(fund_type_counts)} fund types')
print(fund_type_counts)

print(f'{fundCode} is in the list: {fundCode in fund_codes2}')
print(df[df['基金代码'] == fundCode])

# 统计fund_codes1和fund_codes2的差异
# fund_codes1中但不在fund_codes2中的基金代码
diff_fund_codes = list(set(fund_codes1) - set(fund_codes2))
print(f'fund_codes1 only: {diff_fund_codes}')
# fund_codes2中但不在fund_codes1中的基金代码
diff_fund_codes = list(set(fund_codes2) - set(fund_codes1))
print(f'fund_codes2 only: count: {len(diff_fund_codes)} {diff_fund_codes[:10]}')
print("=== sample ====")
print(ak.fund_overview_em(diff_fund_codes[0]))

print('=== 基金概况 via ak.fund_overview_em() ===')
df = ak.fund_overview_em(symbol=fundCode)
print(df)

print("=== 基金累计净值走势 via ak.fund_open_fund_info_em(symbol=fundCode, indicator=\"累计净值走势\") ===")
year_start = '2026-02-20'
year_end = '2026-03-20'
df = ak.fund_open_fund_info_em(symbol=fundCode, indicator='累计净值走势')
start_date = pd.to_datetime(year_start).date()
end_date = pd.to_datetime(year_end).date()
df = df[(df['净值日期'] >= start_date) & (df['净值日期'] <= end_date)]
df = df.sort_values('净值日期').reset_index(drop=True)
print(df)

print("=== 基金单位净值走势 via ak.fund_open_fund_info_em(symbol=fundCode, indicator=\"单位净值走势\") ===")
year_start = '2026-02-20'
year_end = '2026-03-20'
df = ak.fund_open_fund_info_em(symbol=fundCode, indicator='单位净值走势')
start_date = pd.to_datetime(year_start).date()
end_date = pd.to_datetime(year_end).date()
df = df[(df['净值日期'] >= start_date) & (df['净值日期'] <= end_date)]
df = df.sort_values('净值日期').reset_index(drop=True)
print(df)

print("=== 基金基金经理 via ak.fund_open_fund_info_em(symbol=fundCode, indicator=\"基金经理\") ===")
manager_df = ak.fund_open_fund_info_em(symbol=fundCode, indicator='基金经理')
print(manager_df)

print("=== 基金分红送配详情 via ak.fund_open_fund_info_em(symbol=fundCode, indicator=\"分红送配详情\") ===")
actions_df = ak.fund_open_fund_info_em(symbol='161606', indicator='分红送配详情')
print(actions_df)

print("=== 基金申购费率 via ak.fund_fee_em(symbol=fundCode, indicator=\"申购费率\") ===")
fee_df = ak.fund_fee_em(symbol=fundCode, indicator='基金申购费率')
print(fee_df)

print("=== 基金赎回费率 via ak.fund_fee_em(symbol=fundCode, indicator=\"赎回费率\") ===")
fee_df = ak.fund_fee_em(symbol=fundCode, indicator='赎回费率')
print(fee_df)

print("=== 基金交易状态 via ak.fund_fee_em(symbol=fundCode, indicator=\"交易状态\") ===")
fee_df = ak.fund_fee_em(symbol=fundCode, indicator='交易状态')
print(fee_df)
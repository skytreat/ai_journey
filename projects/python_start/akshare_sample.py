import akshare as ak
import pandas as pd
import matplotlib.pyplot as plt

# 设置中文字体和样式
plt.rcParams['font.sans-serif'] = ['SimHei', 'Arial Unicode MS', 'DejaVu Sans']
plt.rcParams['axes.unicode_minus'] = False

# 获取股票数据 - 以茅台为例(600519)
stock_code = "600519"
stock_name = "贵州茅台"

stock_zh_a_history_df = ak.stock_zh_a_hist(symbol=stock_code, period="daily", start_date="20250101", end_date="20250531", adjust="")
# print(stock_zh_a_history_df)
stock_zh_a_history_df.to_csv("000001_daily_20250101_20230531.csv", index=False)

# 数据预处理
stock_zh_a_history_df['日期'] = pd.to_datetime(stock_zh_a_history_df['日期'])
stock_zh_a_history_df.set_index('日期', inplace=True)
stock_zh_a_history_df.sort_index(inplace=True)

print("数据概览:")
print(stock_zh_a_history_df.head())

# 创建画布
plt.figure(figsize=(14, 12))

# 子图1: 价格走势
plt.subplot(3, 1, 1)
plt.plot(stock_zh_a_history_df.index, stock_zh_a_history_df['收盘'], label='收盘价', linewidth=2, color='#1f77b4')
plt.plot(stock_zh_a_history_df.index, stock_zh_a_history_df['开盘'], label='开盘价', alpha=0.7, color='#ff7f0e')
plt.fill_between(stock_zh_a_history_df.index, stock_zh_a_history_df['最低'], stock_zh_a_history_df['最高'], alpha=0.2, label='价格区间', color='grey')
plt.title(f'{stock_name}({stock_code}) - 价格走势', fontsize=15, fontweight='bold')
plt.ylabel('价格 (元)', fontsize=12)
plt.legend()
plt.grid(True, alpha=0.3)

# 子图2: 成交量
plt.subplot(3, 1, 2)
# 根据涨跌设置颜色
colors = ['red' if close >= open else 'green' for close, open in zip(stock_zh_a_history_df['收盘'], stock_zh_a_history_df['开盘'])]
plt.bar(stock_zh_a_history_df.index, stock_zh_a_history_df['成交量'], color=colors, alpha=0.7, label='成交量')
plt.title('成交量', fontsize=13)
plt.ylabel('成交量', fontsize=12)
plt.xlabel('日期', fontsize=12)
plt.legend()
plt.grid(True, alpha=0.3)

# 计算技术指标
stock_zh_a_history_df['MA5'] = stock_zh_a_history_df['收盘'].rolling(window=5).mean()    # 5日均线
stock_zh_a_history_df['MA20'] = stock_zh_a_history_df['收盘'].rolling(window=20).mean()  # 20日均线
stock_zh_a_history_df['MA60'] = stock_zh_a_history_df['收盘'].rolling(window=60).mean()  # 60日均线

# 计算收益率
stock_zh_a_history_df['日收益率'] = stock_zh_a_history_df['收盘'].pct_change() * 100
stock_zh_a_history_df['累计收益率'] = (stock_zh_a_history_df['收盘'] / stock_zh_a_history_df['收盘'].iloc[0] - 1) * 100

# 子图3: 价格与均线
plt.subplot(3, 1, 3)
plt.plot(stock_zh_a_history_df.index, stock_zh_a_history_df['收盘'], label='收盘价', linewidth=2, color='#1f77b4')
plt.plot(stock_zh_a_history_df.index, stock_zh_a_history_df['MA5'], label='5日均线', linewidth=1, color='red', alpha=0.8)
plt.plot(stock_zh_a_history_df.index, stock_zh_a_history_df['MA20'], label='20日均线', linewidth=1, color='orange', alpha=0.8)
plt.plot(stock_zh_a_history_df.index, stock_zh_a_history_df['MA60'], label='60日均线', linewidth=1, color='purple', alpha=0.8)
plt.title(f'{stock_name} - 价格与移动平均线', fontsize=15, fontweight='bold')
plt.ylabel('价格 (元)', fontsize=12)
plt.legend()
plt.grid(True, alpha=0.3)

plt.tight_layout()
plt.show()
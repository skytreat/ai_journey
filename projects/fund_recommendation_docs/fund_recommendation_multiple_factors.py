import akshare as ak
import pandas as pd
import numpy as np
import os
import pickle
import time
import random
import warnings
from datetime import datetime, timedelta
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
import matplotlib.pyplot as plt
import seaborn as sns

warnings.filterwarnings('ignore')
plt.rcParams['font.sans-serif'] = ['SimHei', 'Arial Unicode MS']  # 支持中文
plt.rcParams['axes.unicode_minus'] = False

class FundDataFetcher:
    """AKShare基金净值数据获取器（带智能缓存+并发控制）"""
    
    def __init__(self, cache_dir="fund_nav_cache", max_workers=3, request_delay=(0.8, 1.5)):
        self.cache_dir = Path(cache_dir)
        self.cache_dir.mkdir(exist_ok=True)
        self.max_workers = max_workers
        self.request_delay = request_delay
        self.failed_codes = []
    
    def _get_cache_path(self, code):
        return self.cache_dir / f"{code}_nav.pkl"
    
    def _is_cache_valid(self, cache_path):
        """检查缓存是否今日有效（基金净值每日更新）"""
        if not cache_path.exists():
            return False
        # 检查文件修改时间是否为今天
        mod_time = datetime.fromtimestamp(cache_path.stat().st_mtime)
        return mod_time.date() == datetime.today().date()
    
    def _save_to_cache(self, code, df):
        """保存净值数据+元信息到缓存"""
        cache_data = {
            'data': df,
            'fetched_time': datetime.now(),
            'code': code
        }
        with open(self._get_cache_path(code), 'wb') as f:
            pickle.dump(cache_data, f)
    
    def _load_from_cache(self, code):
        """从缓存加载数据"""
        cache_path = self._get_cache_path(code)
        if self._is_cache_valid(cache_path):
            try:
                with open(cache_path, 'rb') as f:
                    return pickle.load(f)['data']
            except Exception as e:
                print(f"  ⚠ 缓存读取失败 {code}: {e}")
        return None
    
    def fetch_single_fund(self, code, force_update=False):
        """
        获取单只基金净值数据
        返回: (code, DataFrame) 或 (code, None) 失败时
        """
        # 1. 检查缓存
        if not force_update:
            cached = self._load_from_cache(code)
            if cached is not None:
                print(f"  ✓ 缓存命中: {code}")
                return code, cached
        
        # 2. 随机延迟防封
        time.sleep(random.uniform(*self.request_delay))
        
        # 3. 调用AKShare接口（带重试）
        max_retries = 3
        for attempt in range(max_retries):
            try:
                # AKShare要求symbol为字符串
                df = ak.fund_open_fund_info_em(symbol=str(code), indicator="单位净值走势")
                
                # 验证数据有效性
                if df.empty or '单位净值' not in df.columns or '净值日期' not in df.columns:
                    raise ValueError("数据结构异常")
                
                # 数据清洗
                df = df[['净值日期', '单位净值']].copy()
                df['净值日期'] = pd.to_datetime(df['净值日期'])
                df = df.sort_values('净值日期').drop_duplicates(subset='净值日期', keep='last')
                df = df.set_index('净值日期')['单位净值']
                df = df[~df.isin([0, np.nan])]  # 清理无效净值
                
                # 保存缓存
                self._save_to_cache(code, df)
                print(f"  ✓ 获取成功: {code} | 数据点: {len(df)}")
                return code, df
                
            except Exception as e:
                if attempt == max_retries - 1:
                    print(f"  ✗ 获取失败 {code} (最终): {str(e)[:50]}")
                    self.failed_codes.append(code)
                    return code, None
                time.sleep(2 ** attempt)  # 指数退避重试
    
    def fetch_multiple_funds(self, fund_codes, force_update=False):
        """
        并发获取多只基金净值数据
        返回: DataFrame (index=日期, columns=基金代码)
        """
        print(f"\n🚀 开始获取 {len(fund_codes)} 只基金净值数据 (线程数: {self.max_workers})")
        print(f"   缓存目录: {self.cache_dir.absolute()}")
        
        results = {}
        with ThreadPoolExecutor(max_workers=self.max_workers) as executor:
            futures = {
                executor.submit(self.fetch_single_fund, code, force_update): code 
                for code in fund_codes
            }
            
            completed = 0
            for future in as_completed(futures):
                code, data = future.result()
                if data is not None:
                    results[code] = data
                completed += 1
                if completed % 10 == 0 or completed == len(fund_codes):
                    print(f"   进度: {completed}/{len(fund_codes)} | 成功: {len(results)} | 失败: {len(self.failed_codes)}")
        
        # 合并数据：按日期对齐，保留各基金有效区间
        if not results:
            raise ValueError("❌ 无有效基金数据，请检查基金代码或网络连接")
        
        merged = pd.DataFrame(results)
        merged = merged.sort_index()
        print(f"\n✅ 数据合并完成 | 日期范围: {merged.index[0].date()} 至 {merged.index[-1].date()}")
        print(f"   有效基金: {merged.shape[1]} | 总交易日: {len(merged)}")
        
        if self.failed_codes:
            print(f"   ⚠ 失败基金 ({len(self.failed_codes)}): {', '.join(self.failed_codes[:5])}{'...' if len(self.failed_codes)>5 else ''}")
        
        return merged


class FundEvaluator:
    """场外基金多因子量化评估系统（适配AKShare数据）"""
    
    def __init__(self, nav_df, risk_free_rate=0.025, min_days=500):
        """
        参数:
            nav_df: DataFrame (index=日期, columns=基金代码, values=单位净值)
            risk_free_rate: 年化无风险利率
            min_days: 最小有效交易日（建议≥2年）
        """
        self.nav_data = nav_df.copy()
        self.risk_free_rate = risk_free_rate
        self.min_days = min_days
        self.metrics = None
        self.scores = None
        # 指标权重配置（可调整）
        self.weights = {
            '年化收益率': 0.25,
            '卡玛比率': 0.15,
            '最大回撤': 0.20,   # 负向指标
            '下行标准差': 0.15, # 负向指标
            '索提诺比率': 0.15,
            '收益波动率': 0.10  # 负向指标
        }
    
    def preprocess(self):
        """数据清洗：处理缺失、对齐、过滤短数据基金"""
        # 删除全空列
        self.nav_data = self.nav_data.dropna(how='all', axis=1)
        
        # 前向填充少量缺失（场外基金通常无缺失，谨慎使用）
        self.nav_data = self.nav_data.fillna(method='ffill', limit=3)
        
        # 过滤数据长度不足的基金
        valid_cols = [
            col for col in self.nav_data.columns 
            if self.nav_data[col].notna().sum() >= self.min_days
        ]
        self.nav_data = self.nav_data[valid_cols]
        
        print(f"✓ 预处理完成 | 有效基金: {len(valid_cols)} | 日期范围: {self.nav_data.index[0].date()} 至 {self.nav_data.index[-1].date()}")
        return self
    
    def _calculate_metrics(self, nav_series):
        """计算单基金核心指标"""
        returns = nav_series.pct_change().dropna()
        if len(returns) < self.min_days:
            return None
        
        # 年化参数
        total_days = (nav_series.index[-1] - nav_series.index[0]).days
        annual_factor = 252 / total_days if total_days > 0 else 252
        
        # 1. 年化收益率
        total_return = (nav_series.iloc[-1] / nav_series.iloc[0]) - 1
        annual_return = (1 + total_return) ** annual_factor - 1
        
        # 2. 波动率
        annual_vol = returns.std() * np.sqrt(252)
        
        # 3. 最大回撤
        cum_return = (1 + returns).cumprod()
        running_max = cum_return.expanding().max()
        drawdown = (cum_return - running_max) / running_max
        max_dd = drawdown.min()
        
        # 4. 下行标准差
        downside_returns = returns[returns < 0]
        downside_std = downside_returns.std() * np.sqrt(252) if len(downside_returns) > 1 else np.nan
        
        # 5. 索提诺比率
        sortino = (annual_return - self.risk_free_rate) / downside_std if downside_std and downside_std > 0 else np.nan
        
        # 6. 卡玛比率
        calmar = annual_return / abs(max_dd) if max_dd and max_dd != 0 else np.nan
        
        return {
            '年化收益率': annual_return,
            '收益波动率': annual_vol,
            '最大回撤': max_dd,
            '下行标准差': downside_std,
            '索提诺比率': sortino,
            '卡玛比率': calmar,
            '数据天数': len(returns),
            '成立日期': nav_series.index[0].date(),
            '最新净值日期': nav_series.index[-1].date(),
            '最新单位净值': nav_series.iloc[-1]
        }
    
    def evaluate_all(self):
        """批量计算所有基金指标"""
        metrics = {}
        for code in self.nav_data.columns:
            nav = self.nav_data[code].dropna()
            if len(nav) >= self.min_days:
                m = self._calculate_metrics(nav)
                if m:
                    metrics[code] = m
        
        self.metrics = pd.DataFrame(metrics).T
        print(f"✓ 指标计算完成 | 评估基金: {len(self.metrics)}")
        return self.metrics
    
    def calculate_scores(self):
        """计算综合得分与星级"""
        if self.metrics is None:
            self.evaluate_all()
        
        # 复制指标并处理负向指标
        df = self.metrics[list(self.weights.keys())].copy()
        negative_metrics = ['最大回撤', '下行标准差', '收益波动率']
        for col in negative_metrics:
            if col in df.columns:
                df[col] = -df[col]  # 转为正向（值越大越好）
        
        # 缩尾处理（±3σ）防异常值
        for col in df.columns:
            mean, std = df[col].mean(), df[col].std()
            lower, upper = mean - 3*std, mean + 3*std
            df[col] = df[col].clip(lower, upper)
        
        # Z-score标准化
        df_norm = (df - df.mean()) / df.std()
        
        # 加权得分
        scores = np.zeros(len(df_norm))
        for col, weight in self.weights.items():
            scores += df_norm[col] * weight
        
        # 生成结果
        result = self.metrics.copy()
        result['综合得分'] = scores
        result['综合排名'] = result['综合得分'].rank(ascending=False, method='dense').astype(int)
        
        # 星级评定（5星制）
        thresholds = np.percentile(result['综合得分'], [20, 40, 60, 80])
        result['星级'] = pd.cut(
            result['综合得分'],
            bins=[-np.inf] + list(thresholds) + [np.inf],
            labels=[1, 2, 3, 4, 5],
            include_lowest=True
        ).astype(int)
        
        self.scores = result.sort_values('综合得分', ascending=False)
        print(f"✓ 评分完成 | 五星基金: {(result['星级']==5).sum()} | 四星+: {(result['星级']>=4).sum()}")
        return self.scores
    
    def get_recommendations(self, top_n=10, min_star=4):
        """获取推荐列表"""
        if self.scores is None:
            self.calculate_scores()
        
        rec = self.scores[self.scores['星级'] >= min_star].head(top_n).copy()
        # 格式化输出
        fmt_cols = ['年化收益率', '最大回撤']
        for col in fmt_cols:
            if col in rec.columns:
                rec[col] = rec[col].apply(lambda x: f"{x:.2%}")
        for col in ['索提诺比率', '卡玛比率']:
            if col in rec.columns:
                rec[col] = rec[col].apply(lambda x: f"{x:.2f}" if pd.notna(x) else "N/A")
        return rec[['星级', '综合排名', '年化收益率', '最大回撤', '索提诺比率', 
                   '卡玛比率', '成立日期', '最新净值日期', '最新单位净值']]
    
    def generate_report(self, top_k=5, save_dir="fund_report"):
        """生成完整评估报告（含可视化）"""
        Path(save_dir).mkdir(exist_ok=True)
        
        # 1. 导出推荐列表CSV
        rec = self.get_recommendations(top_n=50, min_star=3)
        csv_path = Path(save_dir) / f"基金推荐_{datetime.now().strftime('%Y%m%d')}.csv"
        rec.to_csv(csv_path, encoding='utf_8_sig')
        print(f"\n📄 推荐列表已保存: {csv_path}")
        
        # 2. 生成首推基金诊断图
        if len(self.scores) > 0:
            top_code = self.scores.index[0]
            self._plot_fund_diagnosis(top_code, save_dir)
        
        # 3. 生成综合评分分布图
        self._plot_score_distribution(save_dir)
        
        print(f"📊 诊断报告已保存至: {save_dir}/")
        return csv_path
    
    def _plot_fund_diagnosis(self, fund_code, save_dir):
        """生成单基金诊断图"""
        if fund_code not in self.nav_data.columns:
            return
        
        nav = self.nav_data[fund_code].dropna()
        returns = nav.pct_change().dropna()
        cum_return = (1 + returns).cumprod()
        
        fig, axes = plt.subplots(2, 2, figsize=(14, 10))
        fig.suptitle(f'基金深度诊断: {fund_code}', fontsize=16, fontweight='bold', y=1.02)
        
        # 净值曲线
        axes[0,0].plot(nav.index, nav.values, linewidth=2, color='#1f77b4')
        axes[0,0].set_title('单位净值走势', fontsize=12)
        axes[0,0].grid(True, alpha=0.3)
        axes[0,0].set_ylabel('单位净值')
        
        # 累计收益
        axes[0,1].plot(cum_return.index, cum_return.values, linewidth=2, color='#2ca02c')
        axes[0,1].axhline(1, color='gray', linestyle='--', alpha=0.7)
        axes[0,1].set_title('累计收益率', fontsize=12)
        axes[0,1].grid(True, alpha=0.3)
        axes[0,1].set_ylabel('累计收益')
        
        # 回撤曲线
        running_max = cum_return.expanding().max()
        drawdown = (cum_return - running_max) / running_max
        axes[1,0].fill_between(drawdown.index, drawdown.values, 0, color='#d62728', alpha=0.7)
        axes[1,0].set_title(f'历史回撤 (最大: {drawdown.min():.2%})', fontsize=12)
        axes[1,0].grid(True, alpha=0.3)
        axes[1,0].set_ylabel('回撤幅度')
        axes[1,0].set_ylim(drawdown.min() * 1.1, 0.01)
        
        # 月度收益热力图
        monthly = returns.resample('M').apply(lambda x: (1+x).prod()-1)
        if len(monthly) > 0:
            monthly_df = monthly.to_frame().reset_index()
            monthly_df['Year'] = monthly_df['净值日期'].dt.year
            monthly_df['Month'] = monthly_df['净值日期'].dt.month
            heatmap_data = monthly_df.pivot(index='Year', columns='Month', values=0)
            sns.heatmap(heatmap_data, ax=axes[1,1], cmap='RdYlGn', center=0, 
                        annot=True, fmt='.1%', cbar_kws={'label': '月收益率'},
                        linewidths=0.5, square=True)
            axes[1,1].set_title('月度收益率热力图', fontsize=12)
            axes[1,1].set_ylabel('年份')
            axes[1,1].set_xlabel('月份')
        
        # 添加指标文本框
        if fund_code in self.metrics.index:
            m = self.metrics.loc[fund_code]
            text = (f"年化收益: {m['年化收益率']:.2%}\n"
                    f"最大回撤: {m['最大回撤']:.2%}\n"
                    f"索提诺比率: {m['索提诺比率']:.2f}\n"
                    f"卡玛比率: {m['卡玛比率']:.2f}\n"
                    f"数据天数: {int(m['数据天数'])}")
            props = dict(boxstyle='round', facecolor='wheat', alpha=0.8)
            axes[0,0].text(0.03, 0.97, text, transform=axes[0,0].transAxes, 
                          fontsize=9, verticalalignment='top', bbox=props,
                          family='monospace')
        
        plt.tight_layout()
        img_path = Path(save_dir) / f"诊断_{fund_code}.png"
        plt.savefig(img_path, dpi=150, bbox_inches='tight')
        plt.close()
    
    def _plot_score_distribution(self, save_dir):
        """生成评分分布图"""
        if self.scores is None:
            return
        
        fig, ax = plt.subplots(figsize=(10, 6))
        star_counts = self.scores['星级'].value_counts().sort_index()
        
        colors = ['#ff9999', '#ffcc99', '#ffff99', '#99ff99', '#66cc66']
        bars = ax.bar(star_counts.index.astype(str), star_counts.values, 
                     color=colors, edgecolor='black', width=0.6)
        
        ax.set_title('基金星级分布', fontsize=14, fontweight='bold')
        ax.set_xlabel('星级', fontsize=12)
        ax.set_ylabel('基金数量', fontsize=12)
        ax.grid(axis='y', alpha=0.3)
        
        # 添加数值标签
        for bar in bars:
            height = bar.get_height()
            ax.text(bar.get_x() + bar.get_width()/2., height + 0.5,
                    f'{int(height)}', ha='center', va='bottom', fontsize=11)
        
        plt.tight_layout()
        img_path = Path(save_dir) / "星级分布.png"
        plt.savefig(img_path, dpi=150, bbox_inches='tight')
        plt.close()


# ==================== 使用示例 ====================
if __name__ == "__main__":
    print("="*70)
    print("🤖 场外基金智能筛选系统 | AKShare数据 + 多因子量化评估")
    print("="*70)
    
    # === 步骤1: 配置基金池（示例：混合型+股票型热门基金）===
    # 实际使用时替换为您的目标基金代码列表（场外基金6位代码）
    SAMPLE_FUNDS = [
        "000001", "110022", "161725", "001838", "003096",  # 易方达、华夏等
        "005827", "004047", "001714", "001595", "001043"   # 行业主题基金
    ]
    
    # === 步骤2: 获取净值数据（自动缓存+并发）===
    fetcher = FundDataFetcher(
        cache_dir="fund_nav_cache",
        max_workers=3,          # 建议3-5，避免AKShare限流
        request_delay=(0.8, 1.5) # 随机延迟区间(秒)
    )
    
    try:
        nav_df = fetcher.fetch_multiple_funds(
            fund_codes=SAMPLE_FUNDS,
            force_update=False  # 设为True可强制刷新缓存
        )
    except Exception as e:
        print(f"❌ 数据获取失败: {e}")
        print("💡 提示: 检查网络连接、AKShare版本(建议>=1.12.0)、基金代码有效性")
        exit(1)
    
    # === 步骤3: 量化评估 ===
    evaluator = FundEvaluator(
        nav_df=nav_df,
        risk_free_rate=0.025,  # 无风险利率(2.5%)
        min_days=500           # 要求至少2年有效数据
    )
    evaluator.preprocess().calculate_scores()
    
    # === 步骤4: 获取推荐结果 ===
    print("\n" + "="*70)
    print("🏆 TOP 5 推荐基金（四星及以上）")
    print("="*70)
    recommendations = evaluator.get_recommendations(top_n=5, min_star=4)
    print(recommendations.to_string())
    
    # === 步骤5: 生成完整报告 ===
    report_dir = f"fund_report_{datetime.now().strftime('%Y%m%d_%H%M')}"
    evaluator.generate_report(top_k=5, save_dir=report_dir)
    
    # === 步骤6: 实用建议 ===
    print("\n" + "="*70)
    print("💡 投资行动建议")
    print("="*70)
    print("1️⃣  【深度验证】打开报告目录，查看首推基金诊断图：")
    print(f"    - 检查'历史回撤'曲线是否在您承受范围内")
    print(f"    - 观察'月度收益热力图'是否有持续亏损月份")
    print("\n2️⃣  【人工复核】对推荐基金进行基本面核查：")
    print("    - 基金经理是否变更？(查看基金公告)")
    print("    - 基金规模是否突变？(警惕大额赎回)")
    print("    - 投资策略是否漂移？(对比最新季报)")
    print("\n3️⃣  【分散配置】建议：")
    print("    - 从推荐列表中选择3-5只不同风格基金")
    print("    - 保守型：侧重'最大回撤<15%'的四星+基金")
    print("    - 进取型：关注'卡玛比率>1.0'的成长型基金")
    print("\n4️⃣  【持续跟踪】")
    print("    - 每季度运行本脚本更新评估")
    print("    - 关注'综合排名'变动超10位的基金")
    print("\n⚠️  重要提醒：")
    print("   - 本模型仅基于历史净值，不构成投资建议")
    print("   - 过往业绩不代表未来表现，市场有风险，投资需谨慎")
    print("   - 建议结合自身风险承受能力、投资目标综合决策")
    
    print("\n✅ 系统执行完成！报告目录:", report_dir)
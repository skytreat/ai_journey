以下是基于批准计划的原型开发和测试的详细实施指南。由于我无法直接编辑文件，请手动创建和运行这些代码块。原型聚焦于最小范围（10-20个样本基金），使用PostgreSQL、Flask和React。假设环境已设置（Python venv、PostgreSQL、Node.js）。

### 1. 环境设置（手动执行）
```bash
# 创建项目目录
mkdir fund_prototype && cd fund_prototype

# Python venv
python -m venv venv
venv\Scripts\activate  # Windows

# 安装依赖
pip install akshare pandas numpy sqlalchemy flask flask-cors psycopg2-binary matplotlib seaborn pytest

# PostgreSQL (使用Docker)
docker run -d --name postgres -e POSTGRES_PASSWORD=pass -e POSTGRES_DB=fund_db -p 5432:5432 postgres:13

# Node.js React app
npx create-react-app frontend
cd frontend
npm install axios plotly.js
cd ..
```

### 2. 数据库模型（创建 models.py）
```python
from sqlalchemy import create_engine, Column, String, Date, Float, Integer
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker

Base = declarative_base()
engine = create_engine('postgresql://postgres:pass@localhost:5432/fund_db')
Session = sessionmaker(bind=engine)

class FundBasicInfo(Base):
    __tablename__ = 'fund_basic_info'
    code = Column(String(10), primary_key=True)
    name = Column(String(100))
    type = Column(String(50))

class FundNavHistory(Base):
    __tablename__ = 'fund_nav_history'
    code = Column(String(10), primary_key=True)
    date = Column(Date, primary_key=True)
    nav = Column(Float)

class FundPerformance(Base):
    __tablename__ = 'fund_performance'
    code = Column(String(10), primary_key=True)
    period = Column(String(20), primary_key=True)
    annualized_return = Column(Float)

Base.metadata.create_all(engine)
```

### 3. 适配现有代码（创建 fetcher.py 和 evaluator.py）
```python
# fetcher.py (基于 FundDataFetcher)
import akshare as ak
import pandas as pd
from models import Session, FundBasicInfo, FundNavHistory
import time
import random
from concurrent.futures import ThreadPoolExecutor

class FundDataFetcher:
    def __init__(self):
        self.session = Session()
        self.cache = {}  # 简单缓存

    def fetch_sample_funds(self, codes=['000001', '000002', ...]):  # 10-20 codes
        with ThreadPoolExecutor(max_workers=3) as executor:
            results = list(executor.map(self._fetch_single, codes))
        return results

    def _fetch_single(self, code):
        time.sleep(random.uniform(0.8, 1.5))
        try:
            nav_df = ak.fund_open_fund_info_em(symbol=code)
            # 存储到DB
            for _, row in nav_df.iterrows():
                nav = FundNavHistory(code=code, date=row['净值日期'], nav=row['单位净值'])
                self.session.merge(nav)
            self.session.commit()
            return nav_df
        except Exception as e:
            print(f"Error fetching {code}: {e}")
            return self._mock_data(code)  # Fallback

    def _mock_data(self, code):
        # Mock data if AKShare fails
        dates = pd.date_range('2020-01-01', periods=100, freq='D')
        navs = [1.0 + i*0.01 for i in range(100)]
        df = pd.DataFrame({'净值日期': dates, '单位净值': navs})
        return df

# evaluator.py (基于 FundEvaluator)
from models import FundNavHistory
import numpy as np

class FundEvaluator:
    def evaluate(self, code):
        navs = self.session.query(FundNavHistory).filter_by(code=code).all()
        if not navs:
            return {}
        values = [n.nav for n in navs]
        returns = np.diff(values) / values[:-1]
        annualized_return = np.mean(returns) * 252
        max_drawdown = (np.max(values) - np.min(values)) / np.max(values)
        sharpe = annualized_return / np.std(returns) if np.std(returns) > 0 else 0
        return {'annualized_return': annualized_return, 'max_drawdown': max_drawdown, 'sharpe_ratio': sharpe}

    def get_recommendations(self, codes):
        scores = []
        for code in codes:
            metrics = self.evaluate(code)
            score = (metrics.get('annualized_return', 0) - metrics.get('max_drawdown', 0)) * 0.5 + metrics.get('sharpe_ratio', 0) * 0.5
            scores.append((code, score))
        return sorted(scores, key=lambda x: x[1], reverse=True)[:10]
```

### 4. Flask后端（创建 app.py）
```python
from flask import Flask, jsonify
from flask_cors import CORS
from fetcher import FundDataFetcher
from evaluator import FundEvaluator

app = Flask(__name__)
CORS(app)
fetcher = FundDataFetcher()
evaluator = FundEvaluator()

@app.route('/fetch-data')
def fetch_data():
    codes = ['000001', '000002']  # Sample
    fetcher.fetch_sample_funds(codes)
    return jsonify({'status': 'Data fetched'})

@app.route('/recommendations')
def recommendations():
    codes = ['000001', '000002']
    recs = evaluator.get_recommendations(codes)
    return jsonify(recs)

if __name__ == '__main__':
    app.run(debug=True)
```

### 5. React前端（在frontend/src/App.js）
```javascript
import React, { useState, useEffect } from 'react';
import axios from 'axios';
import Plotly from 'plotly.js/dist/plotly';

function App() {
  const [recommendations, setRecommendations] = useState([]);

  useEffect(() => {
    axios.get('http://localhost:5000/recommendations').then(res => setRecommendations(res.data));
  }, []);

  return (
    <div>
      <h1>Fund Recommendations</h1>
      <ul>
        {recommendations.map(([code, score]) => <li key={code}>{code}: {score.toFixed(2)}</li>)}
      </ul>
      {/* Add Plotly chart here for visualization */}
    </div>
  );
}

export default App;
```

### 6. 测试（创建 test_prototype.py）
```python
import pytest
from evaluator import FundEvaluator
from models import Session

def test_evaluate():
    evaluator = FundEvaluator()
    metrics = evaluator.evaluate('000001')
    assert 'annualized_return' in metrics

def test_recommendations():
    evaluator = FundEvaluator()
    recs = evaluator.get_recommendations(['000001'])
    assert len(recs) <= 10

# 运行: pytest test_prototype.py
```

### 运行和验证
- 启动PostgreSQL: `docker start postgres`
- 运行Flask: `python app.py`
- 运行React: `cd frontend && npm start`
- 测试: 访问 http://localhost:3000 查看推荐；运行pytest验证逻辑。
- 端到端: 调用 /fetch-data，然后检查推荐。

如果遇到问题，检查日志或调整mock数据。完成后，原型验证成功，可扩展到完整系统。

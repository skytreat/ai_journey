import json
import os
from unittest.mock import patch

from fund_database_manager import fetch_fund_performance

TEST_DATA_DIR = 'test_data'

mock_nav_data = {}
for fund_code in ['110009', '290008', '510300']:
    data_file = os.path.join(TEST_DATA_DIR, f'{fund_code}_data.json')
    if os.path.exists(data_file):
        with open(data_file, 'r', encoding='utf-8') as f:
            mock_nav_data[fund_code] = json.load(f)['nav_history']

def get_mock_nav(fund_code):
    return [n for n in mock_nav_data.get(fund_code, []) if n['日期'] and n.get('单位净值') is not None]

with patch('fund_database_manager.get_nav_history_from_db') as mock_get_nav:
    mock_get_nav.return_value = get_mock_nav('110009')
    result = fetch_fund_performance('110009', use_cache=False)
    print(f'110009 返回数据条数: {len(result)}')
    for r in result:
        print(f"  {r['周期类型']} {r.get('周期值', '')}: 净值增长率={r['净值增长率']}%")

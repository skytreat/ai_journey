import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import unittest
import sqlite3
from datetime import datetime, timedelta


def get_previous_workday(date=None):
    if date is None:
        date = datetime.now()
    one_day = timedelta(days=1)
    prev_day = date - one_day
    while prev_day.weekday() >= 5:
        prev_day -= one_day
    return prev_day.strftime("%Y-%m-%d")


from fund_database_manager import (
    update_single_fund,
    get_latest_nav_date,
    get_latest_corporate_action_date,
    get_latest_manager_date,
    fetch_fund_nav_history,
    fetch_fund_managers,
    fetch_corporate_actions,
    DATABASE_PATH
)


@unittest.skip("集成测试，需要真实数据库和API")
class TestFundDatabaseManager(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        fund_code = "007590"
        conn = sqlite3.connect(DATABASE_PATH)
        cursor = conn.cursor()
        cursor.execute('DELETE FROM fund_nav_history WHERE 代码 = ?', (fund_code,))
        cursor.execute('DELETE FROM fund_basic_info WHERE 代码 = ?', (fund_code,))
        cursor.execute('DELETE FROM fund_purchase_status WHERE 代码 = ?', (fund_code,))
        cursor.execute('DELETE FROM fund_redemption_status WHERE 代码 = ?', (fund_code,))
        conn.commit()
        conn.close()

    def test_update_single_fund_007590(self):
        """测试007590基金全量更新，获取完整历史净值"""
        fund_code = "007590"

        result = update_single_fund(fund_code)
        total_records, new_records = result

        self.assertGreater(total_records, 1500, "007590应该有超过1500条净值记录")
        self.assertGreaterEqual(new_records, 0, "新记录数应该>=0")

        latest_nav_date = get_latest_nav_date(fund_code)
        self.assertIsNotNone(latest_nav_date, "应该有最新净值日期")
        self.assertGreaterEqual(latest_nav_date, "2019-01-01", "007590成立于2019年，应该有2019年后的数据")

    def test_fetch_nav_history_without_start_date(self):
        """测试不带start_date参数获取净值历史"""
        fund_code = "007590"
        end_date = get_previous_workday()

        nav_history = fetch_fund_nav_history(fund_code, None, end_date)

        self.assertIsNotNone(nav_history, "应该返回净值数据")
        self.assertGreater(len(nav_history), 1500, "007590应该有超过1500条历史净值")
        self.assertTrue(any(nav["日期"] == "2019-09-18" for nav in nav_history), "应该包含成立日期(2019-09-18)的数据")
        self.assertTrue(any(nav["日期"] == end_date for nav in nav_history), f"应该包含end_date({end_date})的数据")
        self.assertTrue(any(nav["日期"] >= "2019-09-19" for nav in nav_history), "应该包含成立日期(2019-09-18)第二天的数据")

    def test_fetch_managers_without_start_date(self):
        """测试不带start_date参数获取基金经理信息"""
        fund_code = "007590"
        end_date = get_previous_workday()

        managers = fetch_fund_managers(fund_code, None, end_date)

        self.assertIsNotNone(managers, "应该返回基金经理数据")
        self.assertGreater(len(managers), 0, "应该有基金经理记录")

    def test_fetch_corporate_actions_without_start_date(self):
        """测试不带start_date参数获取分红送配信息"""
        fund_code = "007590"
        end_date = get_previous_workday()

        corporate_actions = fetch_corporate_actions(fund_code, None, end_date)

        self.assertIsNotNone(corporate_actions, "应该返回分红送配数据")


if __name__ == "__main__":
    unittest.main()
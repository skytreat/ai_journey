import unittest
import sys
import os
from unittest.mock import Mock, patch
import json

# 添加当前目录到Python路径
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from fund_data_collector import (
    get_fund_basic_info,
    get_fund_nav_history,
    get_fund_performance,
    get_fund_asset_scale,
    get_fund_manager,
    get_fund_corporate_actions,
    clean_fund_basic_info,
    clean_fund_nav_history,
    clean_fund_performance,
    clean_fund_asset_scale,
    clean_fund_manager,
    clean_fund_corporate_actions,
    calculate_adjusted_nav,
    check_data_quality,
    collect_fund_data
)

class TestFundDataCollector(unittest.TestCase):
    
    def test_calculate_adjusted_nav(self):
        """测试复权净值计算"""
        # 测试正常情况
        self.assertEqual(calculate_adjusted_nav(1.0, 1.5), 1.5)
        # 测试边界情况
        self.assertEqual(calculate_adjusted_nav(0, 1.5), 0)
        self.assertEqual(calculate_adjusted_nav(1.0, 0), 1.0)
        self.assertEqual(calculate_adjusted_nav(0, 0), 0)
    
    def test_check_data_quality(self):
        """测试数据质量检查"""
        # 测试空数据
        is_valid, message = check_data_quality(None, "basic_info")
        self.assertFalse(is_valid)
        self.assertEqual(message, "数据为空")
        
        # 测试错误数据
        is_valid, message = check_data_quality({"error": "test error"}, "basic_info")
        self.assertFalse(is_valid)
        self.assertEqual(message, "数据获取错误: test error")
        
        # 测试基本信息数据缺少必要字段
        incomplete_data = {"Code": "000001", "Name": "测试基金"}
        is_valid, message = check_data_quality(incomplete_data, "basic_info")
        self.assertFalse(is_valid)
        self.assertIn("缺少必要字段", message)
        
        # 测试完整的基本信息数据
        complete_data = {
            "Code": "000001",
            "Name": "测试基金",
            "FundType": "混合型",
            "EstablishDate": "2020-01-01"
        }
        is_valid, message = check_data_quality(complete_data, "basic_info")
        self.assertTrue(is_valid)
        self.assertEqual(message, "数据质量检查通过")
        
        # 测试净值历史数据
        is_valid, message = check_data_quality([], "nav_history")
        self.assertFalse(is_valid)
        self.assertEqual(message, "净值历史数据为空")
        
        is_valid, message = check_data_quality([{"Date": "2020-01-01", "Nav": 1.0}], "nav_history")
        self.assertTrue(is_valid)
        self.assertEqual(message, "数据质量检查通过")
    
    def test_clean_fund_basic_info(self):
        """测试基金基本信息清洗"""
        raw_data = {
            "基金简称": "华夏成长混合",
            "基金类型": "混合型",
            "基金经理": "张三",
            "成立日期": "2000-10-09",
            "风险等级": "中风险",
            "基金公司": "华夏基金",
            "托管人": "工商银行",
            "投资风格": "成长型",
            "管理费": 0.015,
            "托管费": 0.0025,
            "申购费": 0.015,
            "赎回费": 0.005,
            "最新净值": 1.2345,
            "累计净值": 2.3456,
            "日增长率": 0.005,
            "周增长率": 0.02,
            "月增长率": 0.05,
            "季增长率": 0.1,
            "年增长率": 0.15,
            "两年增长率": 0.3,
            "三年增长率": 0.45,
            "五年增长率": 0.6,
            "成立以来增长率": 1.3456
        }
        
        cleaned = clean_fund_basic_info(raw_data, "000001")
        
        self.assertEqual(cleaned["Code"], "000001")
        self.assertEqual(cleaned["Name"], "华夏成长混合")
        self.assertEqual(cleaned["FundType"], "混合型")
        self.assertEqual(cleaned["Manager"], "张三")
        self.assertEqual(cleaned["EstablishDate"], "2000-10-09")
        self.assertEqual(cleaned["RiskLevel"], "中风险")
        self.assertEqual(cleaned["Issuer"], "华夏基金")
        self.assertEqual(cleaned["托管人"], "工商银行")
        self.assertEqual(cleaned["InvestmentStyle"], "成长型")
        self.assertEqual(cleaned["ManagementFee"], 0.015)
        self.assertEqual(cleaned["CustodianFee"], 0.0025)
        self.assertEqual(cleaned["PurchaseFee"], 0.015)
        self.assertEqual(cleaned["RedemptionFee"], 0.005)
        self.assertEqual(cleaned["CurrentNav"], 1.2345)
        self.assertEqual(cleaned["AccumulatedNav"], 2.3456)
        self.assertEqual(cleaned["DailyGrowthRate"], 0.005)
        self.assertEqual(cleaned["WeeklyGrowthRate"], 0.02)
        self.assertEqual(cleaned["MonthlyGrowthRate"], 0.05)
        self.assertEqual(cleaned["QuarterlyGrowthRate"], 0.1)
        self.assertEqual(cleaned["YearlyGrowthRate"], 0.15)
        self.assertEqual(cleaned["TwoYearGrowthRate"], 0.3)
        self.assertEqual(cleaned["ThreeYearGrowthRate"], 0.45)
        self.assertEqual(cleaned["FiveYearGrowthRate"], 0.6)
        self.assertEqual(cleaned["SinceEstablishmentGrowthRate"], 1.3456)
        self.assertIn("CreationDate", cleaned)
        self.assertIn("LastUpdateDate", cleaned)
    
    def test_clean_fund_nav_history(self):
        """测试基金净值历史数据清洗"""
        raw_data = {
            "日期": "2024-01-01",
            "单位净值": 1.2345,
            "累计净值": 2.3456,
            "日增长率": 0.005
        }
        
        cleaned = clean_fund_nav_history(raw_data, "000001")
        
        self.assertEqual(cleaned["Code"], "000001")
        self.assertEqual(cleaned["Date"], "2024-01-01")
        self.assertEqual(cleaned["Nav"], 1.2345)
        self.assertEqual(cleaned["AccumulatedNav"], 2.3456)
        self.assertEqual(cleaned["DailyGrowthRate"], 0.005)
        self.assertEqual(cleaned["AdjustedNav"], 2.3456)
        self.assertIn("CreationDate", cleaned)
    
    def test_clean_fund_performance(self):
        """测试基金业绩数据清洗"""
        raw_data = {
            "时期": "近1年",
            "净值增长率": 0.15,
            "最大回撤": 0.2,
            "夏普比率": 1.2
        }
        
        cleaned = clean_fund_performance(raw_data, "000001")
        
        self.assertEqual(cleaned["Code"], "000001")
        self.assertEqual(cleaned["PeriodType"], "近1年")
        self.assertEqual(cleaned["NavGrowthRate"], 0.15)
        self.assertEqual(cleaned["MaxDrawdown"], 0.2)
        self.assertEqual(cleaned["SharpeRatio"], 1.2)
        self.assertIn("CreationDate", cleaned)
    
    def test_clean_fund_asset_scale(self):
        """测试基金资产规模数据清洗"""
        raw_data = {
            "日期": "2024-01-01",
            "资产规模": 10.5
        }
        
        cleaned = clean_fund_asset_scale(raw_data, "000001")
        
        self.assertEqual(cleaned["Code"], "000001")
        self.assertEqual(cleaned["Date"], "2024-01-01")
        self.assertEqual(cleaned["Scale"], 10.5)
        self.assertIn("CreationDate", cleaned)
    
    def test_clean_fund_manager(self):
        """测试基金经理数据清洗"""
        raw_data = {
            "基金经理": "张三",
            "任职时间": "3年",
            "上任日期": "2021-01-01",
            "离任日期": ""
        }
        
        cleaned = clean_fund_manager(raw_data, "000001")
        
        self.assertEqual(cleaned["Code"], "000001")
        self.assertEqual(cleaned["ManagerName"], "张三")
        self.assertEqual(cleaned["Tenure"], "3年")
        self.assertEqual(cleaned["StartDate"], "2021-01-01")
        self.assertEqual(cleaned["EndDate"], "")
        self.assertIn("CreationDate", cleaned)
    
    def test_clean_fund_corporate_actions(self):
        """测试基金公司行为事件数据清洗"""
        raw_data = {
            "公告日期": "2024-01-01",
            "公告类型": "分红",
            "公告内容": "每10份分红0.5元"
        }
        
        cleaned = clean_fund_corporate_actions(raw_data, "000001")
        
        self.assertEqual(cleaned["Code"], "000001")
        self.assertEqual(cleaned["AnnouncementDate"], "2024-01-01")
        self.assertEqual(cleaned["EventType"], "分红")
        self.assertEqual(cleaned["EventContent"], "每10份分红0.5元")
        self.assertIn("CreationDate", cleaned)
    
    @patch('fund_data_collector.ak')
    def test_get_fund_basic_info(self, mock_ak):
        """测试获取基金基本信息"""
        # 模拟akshare返回数据
        mock_df = Mock()
        mock_df.to_dict.return_value = [{"基金简称": "华夏成长混合", "基金类型": "混合型"}]
        mock_ak.fund_info_em.return_value = mock_df
        
        result = get_fund_basic_info("000001")
        
        self.assertEqual(result["Code"], "000001")
        self.assertEqual(result["Name"], "华夏成长混合")
        self.assertEqual(result["FundType"], "混合型")
    
    @patch('fund_data_collector.ak')
    def test_get_fund_nav_history(self, mock_ak):
        """测试获取基金净值历史数据"""
        # 模拟akshare返回数据
        mock_df = Mock()
        mock_df.to_dict.return_value = [{"日期": "2024-01-01", "单位净值": 1.2345}]
        mock_ak.fund_nav_em.return_value = mock_df
        
        result = get_fund_nav_history("000001", "20240101", "20240102")
        
        self.assertIsInstance(result, list)
        self.assertEqual(len(result), 1)
        self.assertEqual(result[0]["Code"], "000001")
        self.assertEqual(result[0]["Date"], "2024-01-01")
        self.assertEqual(result[0]["Nav"], 1.2345)
    
    @patch('fund_data_collector.ak')
    def test_get_fund_performance(self, mock_ak):
        """测试获取基金业绩数据"""
        # 模拟akshare返回数据
        mock_df = Mock()
        mock_df.to_dict.return_value = [{"时期": "近1年", "净值增长率": 0.15}]
        mock_ak.fund_performance_analyze.return_value = mock_df
        
        result = get_fund_performance("000001")
        
        self.assertEqual(result["Code"], "000001")
        self.assertEqual(result["PeriodType"], "近1年")
        self.assertEqual(result["NavGrowthRate"], 0.15)
    
    @patch('fund_data_collector.ak')
    def test_get_fund_asset_scale(self, mock_ak):
        """测试获取基金资产规模数据"""
        # 模拟akshare返回数据
        mock_df = Mock()
        mock_df.to_dict.return_value = [{"日期": "2024-01-01", "资产规模": 10.5}]
        mock_ak.fund_scale.return_value = mock_df
        
        result = get_fund_asset_scale("000001")
        
        self.assertIsInstance(result, list)
        self.assertEqual(len(result), 1)
        self.assertEqual(result[0]["Code"], "000001")
        self.assertEqual(result[0]["Date"], "2024-01-01")
        self.assertEqual(result[0]["Scale"], 10.5)
    
    @patch('fund_data_collector.ak')
    def test_get_fund_manager(self, mock_ak):
        """测试获取基金经理数据"""
        # 模拟akshare返回数据
        mock_df = Mock()
        mock_df.to_dict.return_value = [{"基金经理": "张三", "上任日期": "2021-01-01"}]
        mock_ak.fund_manager.return_value = mock_df
        
        result = get_fund_manager("000001")
        
        self.assertIsInstance(result, list)
        self.assertEqual(len(result), 1)
        self.assertEqual(result[0]["Code"], "000001")
        self.assertEqual(result[0]["ManagerName"], "张三")
        self.assertEqual(result[0]["StartDate"], "2021-01-01")
    
    @patch('fund_data_collector.ak')
    def test_get_fund_corporate_actions(self, mock_ak):
        """测试获取基金公司行为事件数据"""
        # 模拟akshare返回数据
        mock_df = Mock()
        mock_df.to_dict.return_value = [{"公告日期": "2024-01-01", "公告类型": "分红"}]
        mock_ak.fund_announcement.return_value = mock_df
        
        result = get_fund_corporate_actions("000001")
        
        self.assertIsInstance(result, list)
        self.assertEqual(len(result), 1)
        self.assertEqual(result[0]["Code"], "000001")
        self.assertEqual(result[0]["AnnouncementDate"], "2024-01-01")
        self.assertEqual(result[0]["EventType"], "分红")

if __name__ == '__main__':
    unittest.main()

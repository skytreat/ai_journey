import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from fund_database_manager import import_fund_data
import glob

db_path = os.path.join(os.path.dirname(__file__), "fund_database.db")

# print("=== 删除现有数据库 ===")
# if os.path.exists(db_path):
#     os.remove(db_path)
#     print(f"Deleted: {db_path}")

# cache_dir = os.path.join(os.path.dirname(__file__), "cache")
# if os.path.exists(cache_dir):
#     for f in glob.glob(os.path.join(cache_dir, "*.pkl")):
#         os.remove(f)
#     print(f"Cleared cache directory")

print("\n=== 开始全量导入A份额基金 ===")
result = import_fund_data(only_a_share=True)

print(f"\n=== 导入完成 ===")
print(f"Success: {result.get('success')}")
print(f"Total records: {result.get('total_records', 0)}")
print(f"Updated records: {result.get('updated_records', 0)}")
print(f"Elapsed time: {result.get('elapsed_time', 0):.2f} seconds")

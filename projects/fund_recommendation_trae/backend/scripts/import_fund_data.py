import sys
import os
import sqlite3
from fund_database_manager import DATABASE_PATH

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

conn = sqlite3.connect(DATABASE_PATH)
cursor = conn.cursor()
# cursor.execute('DELETE FROM fund_performance')
# cursor.execute('DELETE FROM fund_performance WHERE 代码 = ?', ('110009',))
conn.commit()
print(f'Deleted {cursor.rowcount} records')
conn.close()

from fund_database_manager import update_single_fund, show_status

if __name__ == "__main__":
    # fund_code = "110009"
    # print(f"Updating fund: {fund_code}")

    # inserted, updated = update_single_fund(fund_code)
    # print(f"Inserted: {inserted}, Updated: {updated}")
    # print(f"Fund {fund_code} update completed!")
    db_status = show_status()
    print(db_status)

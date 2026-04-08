import sys
import os
import warnings
warnings.filterwarnings("ignore")

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from fund_database_manager import (
    get_all_fund_codes_fromDb, get_nav_history_from_db, get_basic_info_from_db,
    get_fund_establishment_date, fetch_fund_performance,
    DATABASE_PATH, get_db_connection
)
from concurrent.futures import ProcessPoolExecutor, as_completed
from datetime import datetime

def get_fund_start_date(fund_code, db_path):
    existing_basic = get_basic_info_from_db(fund_code, db_path)
    if existing_basic and existing_basic.get("成立日期"):
        try:
            date_str = str(existing_basic["成立日期"]).replace("年", "-").replace("月", "-").replace("日", "")[:10]
            return datetime.strptime(date_str, "%Y-%m-%d")
        except:
            pass
    return get_fund_establishment_date(fund_code, db_path)

def calculate_single_fund_performance(args):
    fund_code, db_path = args

    nav_data = get_nav_history_from_db(fund_code, db_path)
    if not nav_data or len(nav_data) < 2:
        return fund_code, 0, []

    fund_start_date = get_fund_start_date(fund_code, db_path)
    is_first = True

    performance = fetch_fund_performance(
        fund_code, db_path,
        is_first_insert=is_first,
        fund_start_date=fund_start_date,
        use_cache=False
    )

    return fund_code, len(performance) if performance else 0, performance if performance else []

def bulk_save_performance(all_performance, db_path):
    if not all_performance:
        return 0

    params = [
        (
            str(d.get("代码")), str(d.get("周期类型")), str(d.get("周期值") or ""),
            float(d.get("净值增长率") or 0), float(d.get("最大回撤") or 0),
            float(d.get("下行标准差") or 0), float(d.get("夏普比率") or 0),
            float(d.get("索提诺比率") or 0), float(d.get("卡玛比率") or 0),
            float(d.get("年化收益率") or 0), float(d.get("波动率") or 0),
            int(d.get("同类型基金排名") or 0) if d.get("同类型基金排名") else None,
            int(d.get("同类型基金总数") or 0) if d.get("同类型基金总数") else None,
            str(d.get("更新日期") or "")
        )
        for d in all_performance
        if d.get("代码") and d.get("周期类型")
    ]

    if not params:
        return 0

    with get_db_connection(db_path, optimize=True) as conn:
        cursor = conn.cursor()
        cursor.executemany("""
            INSERT OR REPLACE INTO fund_performance (
                代码, 周期类型, 周期值, 净值增长率, 最大回撤, 下行标准差,
                夏普比率, 索提诺比率, 卡玛比率, 年化收益率, 波动率,
                同类型基金排名, 同类型基金总数, 更新日期
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, params)
        conn.commit()
        return cursor.rowcount

def process_batch(args_list, batch_num, total_batches):
    results = []
    with ProcessPoolExecutor(max_workers=15) as executor:
        futures = {executor.submit(calculate_single_fund_performance, args): args[0] for args in args_list}
        for future in as_completed(futures):
            fund_code = futures[future]
            try:
                code, perf_count, perf_data = future.result()
                results.append((code, perf_count, perf_data))
            except Exception as e:
                results.append((fund_code, 0, []))
    return results

def main():
    BATCH_SIZE = 500

    fund_codes = get_all_fund_codes_fromDb()
    total = len(fund_codes)
    print(f"Recalculating performance for {total} funds (batch mode, batch_size={BATCH_SIZE})")

    with get_db_connection(DATABASE_PATH) as conn:
        cursor = conn.cursor()
        cursor.execute("DELETE FROM fund_performance")
        conn.commit()
        print("Cleared fund_performance table")

    total_batches = (total + BATCH_SIZE - 1) // BATCH_SIZE
    total_records = 0
    total_completed = 0
    total_failed = 0

    for batch_idx in range(total_batches):
        batch_start = batch_idx * BATCH_SIZE
        batch_end = min(batch_start + BATCH_SIZE, total)
        batch_codes = fund_codes[batch_start:batch_end]

        print(f"\nProcessing batch {batch_idx + 1}/{total_batches} ({len(batch_codes)} funds)...")

        args_list = [(code, DATABASE_PATH) for code in batch_codes]
        batch_results = process_batch(args_list, batch_idx + 1, total_batches)

        batch_performance = []
        for code, perf_count, perf_data in batch_results:
            if perf_data:
                batch_performance.extend(perf_data)
            total_completed += 1

        if batch_performance:
            saved = bulk_save_performance(batch_performance, DATABASE_PATH)
            total_records += saved
            print(f"  Saved {saved} records (batch {batch_idx + 1}/{total_batches})")

        del batch_performance

    print(f"\n=== Completed ===")
    print(f"Total funds: {total}")
    print(f"Completed: {total_completed}")
    print(f"Failed: {total_failed}")
    print(f"Performance records saved: {total_records}")

if __name__ == "__main__":
    main()
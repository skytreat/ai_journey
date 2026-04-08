import os
from fund_database_manager import create_database
import sqlite3
import time
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor, as_completed
import sys
sys.path.insert(0, '.')

from fund_database_manager import (
    fetch_fund_list_from_akshare,
    fetch_fund_basic_info,
    fetch_fund_nav_history,
    save_basic_info_batch,
    save_nav_history,
    fetch_fund_performance,
    save_performance,
    get_all_fund_codes_fromDb,
    acquire_update_lock,
    release_update_lock,
    record_update_history,
    get_db_connection,
    logger,
    DATABASE_PATH
)

BATCH_SIZE = 100

def batch_get_establishment_dates(fund_codes, db_path):
    if not fund_codes:
        return {}
    result = {}
    with get_db_connection(db_path) as conn:
        cursor = conn.cursor()
        placeholders = ",".join(["?"] * len(fund_codes))
        cursor.execute(f"SELECT 代码, 成立日期 FROM fund_basic_info WHERE 代码 IN ({placeholders})", fund_codes)
        for code, est_date in cursor.fetchall():
            if est_date:
                try:
                    date_str = str(est_date).replace("年", "-").replace("月", "-").replace("日", "")[:10]
                    result[code] = datetime.strptime(date_str, "%Y-%m-%d")
                except:
                    result[code] = None
            else:
                result[code] = None
    return result

def import_fund_data_batch_streaming(
    max_workers: int = 50,
    db_path: str = DATABASE_PATH,
    limit: int = None,
    only_a_share: bool = True,
    use_cache: bool = True
):
    print("开始流式批量导入...")
    start_time = time.time()

    if not acquire_update_lock():
        return {"success": False, "error": "获取更新锁失败"}

    try:
        all_fund_codes = fetch_fund_list_from_akshare(only_a_share=only_a_share, use_cache=use_cache)
        if limit:
            all_fund_codes = sorted(all_fund_codes)[:limit]

        existing_codes = set(get_all_fund_codes_fromDb(db_path))
        new_fund_codes = [code for code in all_fund_codes if code not in existing_codes]
        update_fund_codes = [code for code in all_fund_codes if code in existing_codes]

        print(f"总基金数: {len(all_fund_codes)}, 新基金: {len(new_fund_codes)}, 更新: {len(update_fund_codes)}")

        total_records = 0
        new_records = 0

        fund_info_dict = {}
        if new_fund_codes:
            print("阶段1: 获取新基金基本信息...")
            basic_infos = []
            for i in range(0, len(new_fund_codes), BATCH_SIZE):
                batch = new_fund_codes[i:i+BATCH_SIZE]
                with ThreadPoolExecutor(max_workers=max_workers) as executor:
                    futures = {
                        executor.submit(fetch_fund_basic_info, code, db_path, True): code
                        for code in batch
                    }
                    for future in as_completed(futures):
                        code = futures[future]
                        try:
                            info = future.result()
                            if info:
                                info["代码"] = code
                                basic_infos.append(info)
                                est_date = info.get("成立日期")
                                if est_date:
                                    est_date = str(est_date).replace("年", "-").replace("月", "-").replace("日", "")[:10]
                                fund_info_dict[code] = {"成立日期": est_date}
                        except Exception as e:
                            logger.warning(f"获取基金 {code} 基本信息失败: {e}")
                if basic_infos:
                    saved = save_basic_info_batch(basic_infos, db_path)
                    total_records += saved
                    new_records += saved
                    print(f"  批次 {i//BATCH_SIZE + 1}: 保存了 {saved} 条基本信息")
                    basic_infos.clear()

        if update_fund_codes:
            print("阶段1.5: 批量获取更新基金的成立日期...")
            update_dates = batch_get_establishment_dates(update_fund_codes, db_path)
            for code, est_date in update_dates.items():
                fund_info_dict[code] = {"成立日期": est_date.strftime("%Y-%m-%d") if est_date else None}

        if new_fund_codes:
            nav_result = process_nav_batch(new_fund_codes, db_path, max_workers, "新基金", fund_info_dict)
            new_records += nav_result["nav_count"]
            total_records += nav_result["perf_count"]
        if update_fund_codes:
            nav_result = process_nav_batch(update_fund_codes, db_path, max_workers, "更新基金", fund_info_dict)
            total_records += nav_result["perf_count"]

        elapsed = int(time.time() - start_time)
        print(f"导入完成! 耗时: {elapsed}秒, 总记录: {total_records}")

        return {
            "success": True,
            "total_records": total_records,
            "new_records": new_records,
            "elapsed_time": elapsed
        }

    except Exception as e:
        print(f"导入失败: {e}")
        import traceback
        traceback.print_exc()
        return {"success": False, "error": str(e)}
    finally:
        release_update_lock()


def process_nav_batch(fund_codes, db_path, max_workers, desc, fund_info_dict=None, is_first_insert=True):
    print(f"阶段: 获取{desc}净值历史（分批）...")
    all_performance = []
    total_nav = 0
    for i in range(0, len(fund_codes), BATCH_SIZE):
        batch = fund_codes[i:i+BATCH_SIZE]
        batch_perf, batch_nav = process_fund_batch(batch, db_path, max_workers, fund_info_dict, is_first_insert)
        all_performance.extend(batch_perf)
        total_nav += batch_nav
        print(f"  批次 {i//BATCH_SIZE + 1}: 处理 {len(batch)} 个基金, 净值 {batch_nav} 条")
    perf_count = 0
    if all_performance:
        perf_count = save_performance(all_performance, db_path)
        print(f"  保存了 {perf_count} 条{desc}绩效")
    return {"nav_count": total_nav, "perf_count": perf_count}


def process_fund_batch(fund_codes, db_path, max_workers, fund_info_dict=None, is_first_insert=True):
    all_performance = []
    all_nav_data = []
    total_nav = 0

    def process_one(code):
        try:
            fund_start_date = None
            if fund_info_dict and code in fund_info_dict:
                date_str = fund_info_dict[code].get("成立日期")
                if date_str:
                    fund_start_date = datetime.strptime(date_str, "%Y-%m-%d")
            nav_data = fetch_fund_nav_history(
                fund_code=code,
                start_date=None,
                end_date=None,
                fund_start_date=fund_start_date,
                use_cache=False,
                db_path=db_path
            )
            perf = fetch_fund_performance(
                fund_code=code,
                db_path=db_path,
                use_cache=False,
                nav_data=nav_data,
                is_first_insert=is_first_insert
            )
            return code, nav_data, perf
        except Exception as e:
            logger.warning(f"处理基金 {code} 净值/绩效失败: {e}")
            return code, None, None

    with ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = {executor.submit(process_one, code): code for code in fund_codes}
        for future in as_completed(futures):
            code, nav_data, perf = future.result()
            if nav_data:
                all_nav_data.extend(nav_data)
            if perf:
                all_performance.extend(perf)

    if all_nav_data:
        total_nav = save_nav_history(all_nav_data, db_path)

    return all_performance, total_nav


if __name__ == "__main__":
    NEW_DB_PATH = os.path.join(os.path.dirname(__file__), "fund_data_test_streaming.db")
    if os.path.exists(NEW_DB_PATH):
        os.remove(NEW_DB_PATH)
        print(f"Deleted: {NEW_DB_PATH}")
    
    create_database(NEW_DB_PATH)

    result = import_fund_data_batch_streaming(max_workers=20, 
                                              limit=50, 
                                              db_path=NEW_DB_PATH,
                                              only_a_share=True,
                                              use_cache=False)
    print(result)
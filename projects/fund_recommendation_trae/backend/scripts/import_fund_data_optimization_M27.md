# import_fund_data 处理逻辑文档

## 一、整体架构

```
import_fund_data()
    │
    ├── 1. 获取基金列表 (fetch_fund_list_from_akshare)
    │
    ├── 2. 分类基金 (FundImporter._prepare_fund_lists)
    │       ├── 新基金 → new_fund_codes
    │       ├── 需更新 → funds_need_update
    │       └── 已完成 → funds_skip
    │
    ├── 3. 执行更新 (FundImporter._execute_updates)
    │       ├── _process_new_funds()   → 并发20
    │       └── _process_update_funds() → 并发10
    │              └── update_single_fund()
    │                     ├── 基本信息
    │                     ├── 净值历史
    │                     └── 绩效数据
    │
    └── 4. 完成记录 (update_update_history)
```

## 二、详细处理流程

### 阶段1：获取基金列表

```python
all_fund_codes = fetch_fund_list_from_akshare(only_a_share, use_cache)
# 返回: ['000001', '000002', ...] (已排序)
```

### 阶段2：分类基金

```python
new_fund_codes = [code for code in all_fund_codes if code not in existing_codes]
funds_need_update = [code for code in funds_to_update if needs_update]
funds_skip = [code for code in funds_to_update if not needs_update]
```

**判断依据**：
- 净值日期 < 昨天 → 需要更新
- 绩效记录不完整 → 需要更新

### 阶段3：处理新基金 (并发20)

```python
for batch in batches(100):
    ThreadPoolExecutor(max_workers=20):
        update_single_fund(code)  # 每个基金独立处理
```

### 阶段4：处理需更新基金 (并发10)

```python
for batch in batches(100):
    ThreadPoolExecutor(max_workers=10):
        update_single_fund(code)  # 每个基金独立处理
```

## 三、update_single_fund 单基金处理逻辑

```
1. 获取基本信息
   ├── 数据库有 → 直接使用
   └── 数据库无 → fetch_fund_basic_info() → save_basic_info()

2. 计算基金成立日期
   ├── 基本信息中有 → 解析
   └── 基本信息中无 → get_fund_establishment_date()

3. 更新净值历史
   ├── get_latest_nav_date() → 获取最新日期
   ├── fetch_fund_nav_history(start, end) → 从akshare获取
   └── save_nav_history() → 批量插入

4. 计算绩效数据 (仅A股)
   ├── is_performance_complete() → 检查完整性
   ├── fetch_fund_performance() → 从DB查询净值 → 计算
   └── save_performance() → 批量插入
```

## 四、数据库操作汇总

| 操作 | 函数 | 事务模式 |
|------|------|---------|
| 基本信息 | `save_basic_info()` | 单条插入，单独事务 |
| 净值历史 | `save_nav_history()` | 批量插入，单个基金一个事务 |
| 绩效数据 | `save_performance()` | 批量插入，单个基金一个事务 |

## 五、性能瓶颈分析

| 瓶颈 | 位置 | 影响 |
|------|------|------|
| **1. 重复数据库查询** | `fetch_fund_performance` 内部调用 `get_nav_history_from_db` | 每个基金多一次 DB 查询 |
| **2. 并发数限制** | 新基金20，更新基金10 | CPU/IO 利用率不均衡 |
| **3. 串行绩效计算** | CPU 密集型在线程中执行 | GIL 限制无法真正并行 |
| **4. 批次间同步** | `as_completed()` 等待所有线程完成 | 批次越小，同步开销越大 |

## 六、性能优化建议

### 优化1：传入 nav_data 避免重复查询

```python
# 当前：fetch_fund_performance 从 DB 查询净值
performance = fetch_fund_performance(fund_code, db_path, ...)

# 优化：fetch_fund_performance 直接使用已获取的 nav_data
performance = fetch_fund_performance(fund_code, db_path, nav_data=nav_data, ...)
```

**预期提升**：减少 50% 的数据库查询

### 优化2：增大更新基金并发数

```python
# 当前：更新基金只用10并发
max_workers = self.max_workers  # =10

# 优化：更新基金也用20并发
max_workers = min(self.max_workers * 2, 20)
```

**预期提升**：更新阶段快 50-100%

### 优化3：使用进程池处理绩效计算

```python
# 当前：线程池处理 CPU 密集型任务
with ThreadPoolExecutor(max_workers=10):
    future = executor.submit(calculate_performance, nav_data)

# 优化：进程池处理 CPU 密集型任务
from concurrent.futures import ProcessPoolExecutor
with ProcessPoolExecutor(max_workers=multiprocessing.cpu_count()):
    future = executor.submit(calculate_performance, nav_data)
```

**预期提升**：绕过 GIL，CPU 利用率 100%

### 优化4：减小批次大小提高并发效率

```python
# 当前：批次100，线程20 → 5批
batch_size = 100

# 优化：批次20，线程20 → 更细粒度并行
batch_size = 20
```

**预期提升**：任务分配更均匀，减少等待

### 优化5：异步数据库写入

```python
# 当前：同步等待数据库写入
for fund in funds:
    save_nav_history(nav_data)  # 阻塞等待

# 优化：异步批量写入
with ThreadPoolExecutor(max_workers=4) as writer:
    futures = [writer.submit(save_nav_history, data) for data in all_data]
```

**预期提升**：IO 与计算重叠

## 七、优化优先级建议

| 优先级 | 优化项 | 复杂度 | 预期提升 |
|-------|--------|-------|---------|
| **P0** | 优化1：传入 nav_data | 低 | 50% 查询减少 |
| **P1** | 优化2：增大并发数 | 低 | 50-100% |
| **P2** | 优化4：减小批次 | 低 | 10-20% |
| **P3** | 优化3：进程池 | 中 | 20-30% |
| **P4** | 优化5：异步写入 | 高 | 依赖系统 |

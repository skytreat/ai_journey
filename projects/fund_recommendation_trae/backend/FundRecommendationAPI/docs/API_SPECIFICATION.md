# FundRecommendationAPI 接口规范文档

## 1. 接口概述

### 1.1 基本信息

| 项目 | 说明 |
|------|------|
| Base URL | `http://localhost:5000/api` |
| 数据格式 | JSON |
| 字符编码 | UTF-8 |
| 认证方式 | V1版本暂无需认证（单用户模式） |

### 1.2 通用响应格式

所有接口均返回统一格式的JSON响应：

```json
{
  "code": 200,
  "message": "success",
  "data": {},
  "timestamp": "2024-03-15T10:30:00Z",
  "request_id": "req_abc123"
}
```

### 1.3 错误响应格式

```json
{
  "code": 400,
  "message": "请求参数错误",
  "data": null,
  "details": "参数'fund_code'不能为空",
  "timestamp": "2024-03-15T10:30:00Z",
  "request_id": "req_abc123"
}
```

### 1.4 错误码定义

| 错误码 | 说明 | 常见场景 |
|--------|------|----------|
| 200 | 成功 | 请求正常处理 |
| 400 | 请求参数错误 | 参数缺失、格式错误、范围超限 |
| 401 | 未授权 | V2版本多用户模式使用 |
| 403 | 禁止访问 | 无权限访问该资源 |
| 404 | 资源不存在 | 基金代码不存在、接口路径错误 |
| 409 | 资源冲突 | 基金已在自选列表中、重复添加 |
| 422 | 请求实体错误 | 业务逻辑校验失败 |
| 429 | 请求过于频繁 | 超过API限流阈值 |
| 500 | 服务器内部错误 | 数据库连接失败、代码异常 |
| 502 | 网关错误 | 后端服务不可用 |
| 503 | 服务暂不可用 | 数据更新中、系统维护 |
| 504 | 网关超时 | 请求处理超时 |

### 1.5 通用分页参数

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| page | int | 否 | 1 | 页码，从1开始 |
| page_size | int | 否 | 20 | 每页条数，范围1-100 |

### 1.6 分页响应格式

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "total": 1000,
    "page": 1,
    "page_size": 20,
    "total_pages": 50,
    "list": []
  }
}
```

---

## 2. 基金查询接口

### 2.1 获取基金列表

**URL**: `GET /api/funds`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| page | int | 否 | 页码，默认1 |
| page_size | int | 否 | 每页条数，默认20 |
| fund_type | string | 否 | 基金类型筛选 |
| risk_level | string | 否 | 风险等级筛选 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "total": 1000,
    "page": 1,
    "page_size": 20,
    "total_pages": 50,
    "list": [
      {
        "code": "000001",
        "name": "华夏成长混合",
        "fund_type": "混合型",
        "nav": 1.2345,
        "accumulated_nav": 2.3456,
        "daily_growth_rate": 0.0123,
        "update_time": "2024-03-15"
      }
    ]
  }
}
```

### 2.2 获取基金详情

**URL**: `GET /api/funds/{code}`

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| code | string | 是 | 基金代码（6位数字） |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "code": "000001",
    "name": "华夏成长混合",
    "fund_type": "混合型",
    "share_type": "A类",
    "main_fund_code": "000001",
    "manager": "张三",
    "custodian": "工商银行",
    "establish_date": "2019-01-15",
    "risk_level": "中风险",
    "benchmark": "沪深300指数",
    "investment_style": "成长型",
    "management_fee_rate": 0.0150,
    "custody_fee_rate": 0.0025,
    "purchase_fee_rate": 0.0120,
    "redemption_fee_rate": 0.0050
  }
}
```

### 2.3 获取基金净值历史

**URL**: `GET /api/funds/{code}/nav`

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| code | string | 是 | 基金代码（6位数字） |

**查询参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| start_date | string | 否 | 开始日期 (YYYY-MM-DD) |
| end_date | string | 否 | 结束日期 (YYYY-MM-DD) |
| limit | int | 否 | 返回条数限制，默认100 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "code": "000001",
      "date": "2024-03-15",
      "nav": 1.2345,
      "accumulated_nav": 2.3456,
      "daily_growth_rate": 0.0123
    }
  ]
}
```

### 2.4 获取基金业绩指标

**URL**: `GET /api/funds/{code}/performance`

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| code | string | 是 | 基金代码（6位数字） |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "period_type": "近1月",
      "nav_growth_rate": 5.23,
      "max_drawdown": 2.15,
      "sharpe_ratio": 1.25,
      "annual_return": 15.80,
      "volatility": 12.30,
      "rank_in_category": 156,
      "total_in_category": 1200
    }
  ]
}
```

### 2.5 获取基金经理信息

**URL**: `GET /api/funds/{code}/managers`

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| code | string | 是 | 基金代码（6位数字） |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "manager_name": "张三",
      "start_date": "2020-01-15",
      "end_date": null,
      "tenure_years": 3.2
    }
  ]
}
```

### 2.6 获取基金资产规模

**URL**: `GET /api/funds/{code}/scale`

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| code | string | 是 | 基金代码（6位数字） |

**查询参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| start_date | string | 否 | 开始日期 (YYYY-MM-DD) |
| end_date | string | 否 | 结束日期 (YYYY-MM-DD) |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "code": "000001",
      "date": "2024-02-29",
      "asset_scale": 45.67,
      "share_scale": 38.90
    }
  ]
}
```

---

## 3. 分析接口

### 3.1 单周期基金排名

**URL**: `GET /api/analysis/ranking`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| period | string | 是 | 周期：week/month/quarter/year |
| limit | int | 否 | 返回数量，默认10 |
| order | string | 否 | 排序：asc/desc，默认desc |
| fund_type | string | 否 | 基金类型筛选 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "rank": 1,
      "code": "000001",
      "name": "华夏成长混合",
      "fund_type": "混合型",
      "nav": 1.2345,
      "return_rate": 15.23,
      "max_drawdown": 5.12,
      "sharpe_ratio": 1.35
    }
  ]
}
```

### 3.2 周期变化率排名

**URL**: `GET /api/analysis/change`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| period | string | 是 | 周期：week/month/quarter/year |
| type | string | 否 | 变化类型：absolute/relative，默认absolute |
| limit | int | 否 | 返回数量，默认10 |
| fund_type | string | 否 | 基金类型筛选 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "rank": 1,
      "code": "000001",
      "name": "华夏成长混合",
      "current_return": 15.23,
      "previous_return": 10.15,
      "change_value": 5.08,
      "change_rate": 50.05,
      "trend": "up"
    }
  ]
}
```

### 3.3 多周期一致性筛选

**URL**: `GET /api/analysis/consistency`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| start_date | string | 是 | 开始日期 (YYYY-MM-DD) |
| end_date | string | 是 | 结束日期 (YYYY-MM-DD) |
| period | string | 否 | 周期间隔：month/quarter/year，默认month |
| limit | int | 否 | 返回数量，默认10 |
| threshold | float | 否 | 一致性阈值(0-1)，默认0.5 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "rank": 1,
      "code": "000001",
      "name": "华夏成长混合",
      "consistency_score": 85.5,
      "appearance_count": 8,
      "total_periods": 10,
      "average_return": 12.35
    }
  ]
}
```

### 3.4 多因子量化评估

**URL**: `GET /api/analysis/multifactor`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| limit | int | 否 | 返回数量，默认10 |
| factors | string[] | 否 | 因子列表：return/risk/riskAdjustedReturn/ranking |
| fund_type | string | 否 | 基金类型筛选 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "rank": 1,
      "code": "000001",
      "name": "华夏成长混合",
      "total_score": 85.6,
      "scores": {
        "return_score": 88.5,
        "risk_score": 82.3,
        "risk_adjusted_return_score": 85.0,
        "ranking_score": 86.2
      },
      "weights": {
        "return": 0.35,
        "risk": 0.30,
        "risk_adjusted_return": 0.25,
        "ranking": 0.10
      }
    }
  ]
}
```

### 3.5 基金对比分析

**URL**: `POST /api/analysis/compare`

**请求体**:

```json
{
  "fund_ids": ["000001", "000002", "000003"]
}
```

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "fund_id": "000001",
      "fund_name": "华夏成长混合",
      "fund_type": "混合型",
      "nav": 1.2345,
      "accumulated_nav": 2.3456,
      "monthly_return": 5.23,
      "quarterly_return": 12.45,
      "yearly_return": 25.80,
      "max_drawdown": 8.15,
      "sharpe_ratio": 1.35
    }
  ]
}
```

---

## 4. 自选基金接口

### 4.1 获取自选基金列表

**URL**: `GET /api/favorites`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| page | int | 否 | 页码，默认1 |
| page_size | int | 否 | 每页条数，默认20 |
| group_id | string | 否 | 分组ID筛选 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "total": 15,
    "page": 1,
    "page_size": 20,
    "list": [
      {
        "id": 1,
        "code": "000001",
        "name": "华夏成长混合",
        "nav": 1.2345,
        "daily_growth_rate": 1.23,
        "added_time": "2024-01-15T10:30:00Z",
        "sort_order": 1,
        "note": "观察中",
        "group_id": "group_1"
      }
    ]
  }
}
```

### 4.2 添加自选基金

**URL**: `POST /api/favorites`

**请求体**:

```json
{
  "code": "000001",
  "note": "观察中",
  "group_id": "group_1"
}
```

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "id": 1,
    "code": "000001",
    "added_time": "2024-03-15T10:30:00Z"
  }
}
```

### 4.3 删除自选基金

**URL**: `DELETE /api/favorites/{code}`

**路径参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| code | string | 是 | 基金代码（6位数字） |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": null
}
```

### 4.4 更新自选基金排序

**URL**: `PUT /api/favorites/sort`

**请求体**:

```json
{
  "fund_codes": ["000001", "000002", "000003"]
}
```

### 4.5 更新自选基金备注

**URL**: `PUT /api/favorites/{code}/note`

**请求体**:

```json
{
  "note": "已持有，等待机会加仓"
}
```

### 4.6 获取自选基金分组

**URL**: `GET /api/favorites/groups`

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "id": "group_1",
      "name": "观察中",
      "count": 5
    },
    {
      "id": "group_2",
      "name": "已持有",
      "count": 3
    }
  ]
}
```

### 4.7 创建自选基金分组

**URL**: `POST /api/favorites/groups`

**请求体**:

```json
{
  "name": "新分组"
}
```

### 4.8 移动基金到分组

**URL**: `PUT /api/favorites/{code}/group`

**请求体**:

```json
{
  "group_id": "group_2"
}
```

---

## 5. 自选基金多因子评分接口

### 5.1 获取自选基金评分

**URL**: `GET /api/favorites/scores`

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "code": "000001",
      "name": "华夏成长混合",
      "total_score": 85.6,
      "rank": 1,
      "scores": {
        "return_score": 88.5,
        "risk_score": 82.3,
        "risk_adjusted_return_score": 85.0,
        "ranking_score": 86.2
      },
      "trend": "up",
      "score_change": 2.5
    }
  ]
}
```

### 5.2 获取评分历史

**URL**: `GET /api/favorites/scores/history`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| code | string | 是 | 基金代码（6位数字） |
| start_date | string | 否 | 开始日期 (YYYY-MM-DD) |
| end_date | string | 否 | 结束日期 (YYYY-MM-DD) |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "date": "2024-03-15",
      "total_score": 85.6,
      "return_score": 88.5,
      "risk_score": 82.3
    }
  ]
}
```

### 5.3 获取权重配置

**URL**: `GET /api/favorites/scores/weights`

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "return_weight": 0.35,
    "risk_weight": 0.30,
    "risk_adjusted_return_weight": 0.25,
    "ranking_weight": 0.10,
    "preset": "balanced"
  }
}
```

### 5.4 更新权重配置

**URL**: `PUT /api/favorites/scores/weights`

**请求体**:

```json
{
  "return_weight": 0.40,
  "risk_weight": 0.25,
  "risk_adjusted_return_weight": 0.25,
  "ranking_weight": 0.10
}
```

**权重约束**:

- 所有权重之和必须等于1.0
- 各权重取值范围：0.05 ~ 0.60

---

## 6. 自定义查询接口

### 6.1 执行KQL查询

**URL**: `POST /api/query/kql`

**请求体**:

```json
{
  "query": "fund_performance | where 基金类型 == \"混合型\" | project 代码, 名称, 年化收益率 | limit 100"
}
```

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "columns": ["代码", "名称", "年化收益率"],
    "rows": [
      ["000001", "华夏成长混合", 15.80]
    ],
    "row_count": 1,
    "execution_time_ms": 125
  }
}
```

**KQL语法限制**:

- V1版本仅支持：`where`, `project`, `limit` 操作符
- 最大返回条数：1000条
- 最大查询时间：5秒
- 禁止操作：DELETE, UPDATE, DROP, INSERT

### 6.2 获取查询历史

**URL**: `GET /api/query/history`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| limit | int | 否 | 返回条数，默认20 |

### 6.3 保存查询模板

**URL**: `POST /api/query/templates`

**请求体**:

```json
{
  "name": "混合型基金业绩查询",
  "query": "fund_performance | where 基金类型 == \"混合型\" | limit 100"
}
```

### 6.4 获取查询模板

**URL**: `GET /api/query/templates`

---

## 7. 元数据接口

### 7.1 获取基金类型列表

**URL**: `GET /api/meta/fund-types`

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    { "value": "股票型", "label": "股票型", "count": 1500 },
    { "value": "债券型", "label": "债券型", "count": 2000 },
    { "value": "混合型", "label": "混合型", "count": 3500 },
    { "value": "货币型", "label": "货币型", "count": 1500 },
    { "value": "指数型", "label": "指数型", "count": 1200 },
    { "value": "QDII", "label": "QDII", "count": 300 }
  ]
}
```

### 7.2 获取基金管理人列表

**URL**: `GET /api/meta/managers`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| page | int | 否 | 页码，默认1 |
| page_size | int | 否 | 每页条数，默认20 |
| keyword | string | 否 | 搜索关键词 |

---

## 8. 系统接口

### 8.1 获取系统状态

**URL**: `GET /api/system/status`

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "status": "running",
    "database_status": "connected",
    "total_funds": 8500,
    "last_update": "2024-03-15T19:30:00Z",
    "update_in_progress": false,
    "cache_stats": {
      "hit_rate": 0.85,
      "total_entries": 150
    }
  }
}
```

### 8.2 触发数据更新

**URL**: `POST /api/system/update`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| type | string | 否 | 更新类型：full/incremental，默认incremental |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "update_id": "update_20240315_001",
    "type": "incremental",
    "started_at": "2024-03-15T19:30:00Z",
    "estimated_duration_minutes": 15
  }
}
```

### 8.3 获取更新历史

**URL**: `GET /api/system/update-history`

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| limit | int | 否 | 返回条数，默认10 |

**响应示例**:

```json
{
  "code": 200,
  "message": "success",
  "data": [
    {
      "id": "update_20240315_001",
      "type": "incremental",
      "status": "completed",
      "started_at": "2024-03-15T19:30:00Z",
      "completed_at": "2024-03-15T19:42:00Z",
      "records_updated": 8500,
      "errors": 0
    }
  ]
}
```

---

## 9. 安全限制

### 9.1 API限流

| 限流策略 | 限制 |
|----------|------|
| 全局限制 | 100次/分钟 |
| KQL查询 | 60次/分钟 |
| 数据更新 | 10次/小时 |

### 9.2 KQL安全限制

- **允许查询的表**:
  - fund_basic_info
  - fund_nav_history
  - fund_performance
  - fund_manager
  - fund_asset_scale

- **禁止的操作**: DELETE, UPDATE, DROP, INSERT, CREATE, ALTER

---

## 10. 性能指标

| 接口场景 | 性能要求 |
|----------|----------|
| 基金列表查询 | < 200ms (P95) |
| 基金详情查询 | < 100ms (P95) |
| 历史净值查询 | < 500ms (P95) |
| 业绩排名计算 | < 5秒 |
| 多因子评分计算 | < 5秒 |
| KQL查询 | < 2秒 |

---

## 11. 文档版本

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2024-03-15 | 初始版本 |

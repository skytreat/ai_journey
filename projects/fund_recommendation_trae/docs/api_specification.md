# 基金推荐系统API接口规范

## 1. 接口基础信息
- **Base URL**: `http://localhost:5000/api/v1`
- **请求格式**: JSON
- **响应格式**: JSON
- **字符编码**: UTF-8

## 2. 通用响应格式

```json
{
  "code": 200,
  "message": "success",
  "data": {}
}
```

## 3. 分页响应格式

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

## 4. 错误响应格式

```json
{
  "code": 400,
  "message": "请求参数错误",
  "details": "参数'fund_code'不能为空",
  "timestamp": "2024-03-15T10:30:00Z",
  "request_id": "req_1234567890"
}
```

## 5. 错误码定义

| 错误码 | 说明 | 常见场景 | 处理建议 |
|--------|------|----------|----------|
| **200** | 成功 | 请求正常处理 | - |
| **400** | 请求参数错误 | 参数缺失、格式错误、范围超限 | 检查请求参数是否符合API文档要求 |
| **401** | 未授权 | 用户未登录或Token过期 | 重新登录获取有效Token |
| **403** | 禁止访问 | 无权限访问该资源 | 检查用户权限或联系管理员 |
| **404** | 资源不存在 | 基金代码不存在、接口路径错误 | 确认基金代码正确或接口路径正确 |
| **409** | 资源冲突 | 基金已在自选列表中、重复添加 | 检查资源是否已存在 |
| **422** | 请求实体错误 | 业务逻辑校验失败 | 检查业务规则（如日期范围） |
| **429** | 请求过于频繁 | 超过API限流阈值 | 降低请求频率，稍后重试 |
| **500** | 服务器内部错误 | 数据库连接失败、代码异常 | 查看服务器日志，联系技术支持 |
| **502** | 网关错误 | 后端服务不可用 | 检查服务状态，稍后重试 |
| **503** | 服务暂不可用 | 数据更新中、系统维护 | 查看系统状态，稍后重试 |
| **504** | 网关超时 | 请求处理超时 | 检查网络状况，稍后重试 |

## 6. 基金查询API接口规范

### 6.1 获取基金列表
- **路径**: `/funds`
- **方法**: `GET`
- **参数**:
  - `page` (可选): 页码，默认1
  - `page_size` (可选): 每页数量，默认20
  - `fund_type` (可选): 基金类型
  - `risk_level` (可选): 风险等级
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "total": 1000,
      "page": 1,
      "page_size": 20,
      "list": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "fund_type": "混合型",
          "share_type": "A",
          "manager": "张三",
          "establish_date": "2001-01-01",
          "risk_level": "中风险",
          "nav": 1.2345,
          "nav_growth_rate": 0.0123
        }
      ]
    }
  }
  ```

### 6.2 获取基金详情
- **路径**: `/funds/{code}`
- **方法**: `GET`
- **参数**:
  - `code` (必填): 基金代码
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "code": "000001",
      "name": "华夏成长混合",
      "fund_type": "混合型",
      "share_type": "A",
      "main_fund_code": "",
      "establish_date": "2001-01-01",
      "list_date": "2001-01-15",
      "manager": "张三",
      "custodian": "中国工商银行",
      "management_fee_rate": 0.015,
      "custodian_fee_rate": 0.0025,
      "sales_fee_rate": 0.001,
      "benchmark": "沪深300指数",
      "tracking_target": "",
      "investment_style": "成长型",
      "risk_level": "中风险",
      "update_time": "2024-03-15T10:30:00Z"
    }
  }
  ```

### 6.3 获取基金净值历史
- **路径**: `/funds/{code}/nav`
- **方法**: `GET`
- **参数**:
  - `code` (必填): 基金代码
  - `start_date` (可选): 开始日期
  - `end_date` (可选): 结束日期
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "code": "000001",
      "nav_history": [
        {
          "date": "2024-01-01",
          "nav": 1.2345,
          "accumulated_nav": 1.5678,
          "daily_growth_rate": 0.005
        }
      ]
    }
  }
  ```

### 6.4 获取基金业绩
- **路径**: `/funds/{code}/performance`
- **方法**: `GET`
- **参数**:
  - `code` (必填): 基金代码
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "code": "000001",
      "performances": [
        {
          "period_type": "近1月",
          "period_value": "",
          "nav_growth_rate": 0.035,
          "max_drawdown": 0.05,
          "downside_std": 0.12,
          "sharpe_ratio": 1.2,
          "sortino_ratio": 1.0,
          "calmar_ratio": 0.8,
          "annual_return": 0.15,
          "volatility": 0.2,
          "rank_in_category": 10,
          "total_in_category": 100
        }
      ]
    }
  }
  ```

### 6.5 获取基金经理
- **路径**: `/funds/{code}/managers`
- **方法**: `GET`
- **参数**:
  - `code` (必填): 基金代码
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "code": "000001",
      "managers": [
        {
          "manager_name": "张三",
          "start_date": "2020-01-01",
          "end_date": null,
          "manage_days": 1500
        }
      ]
    }
  }
  ```

### 6.6 获取基金规模
- **路径**: `/funds/{code}/scale`
- **方法**: `GET`
- **参数**:
  - `code` (必填): 基金代码
  - `start_date` (可选): 开始日期
  - `end_date` (可选): 结束日期
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "code": "000001",
      "scale_history": [
        {
          "date": "2024-01-01",
          "asset_scale": 100.5,
          "share_scale": 80.2
        }
      ]
    }
  }
  ```

## 7. 分析API接口规范

### 7.1 单周期排名
- **路径**: `/analysis/ranking`
- **方法**: `GET`
- **参数**:
  - `fund_type` (必填): 基金类型
  - `period` (必填): 周期：week/month/quarter/year
  - `rank_type` (必填): 排名类型：top/bottom
  - `n` (可选): 排名数量，默认20
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "period": "2024-03",
      "rank_type": "top",
      "list": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "fund_type": "混合型",
          "growth_rate": 0.1523,
          "rank": 1
        }
      ]
    }
  }
  ```

### 7.2 周期变化排名
- **路径**: `/analysis/change`
- **方法**: `GET`
- **参数**:
  - `fund_type` (必填): 基金类型
  - `period` (必填): 周期：week/month/quarter/year
  - `rank_type` (必填): 排名类型：top/bottom
  - `n` (可选): 排名数量，默认20
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "current_period": "2024-03",
      "previous_period": "2024-02",
      "rank_type": "top",
      "list": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "fund_type": "混合型",
          "current_growth_rate": 0.1523,
          "previous_growth_rate": 0.1012,
          "absolute_change": 0.0511,
          "relative_change": 0.505,
          "trend": "上升"
        }
      ]
    }
  }
  ```

### 7.3 多周期一致性
- **路径**: `/analysis/consistency`
- **方法**: `GET`
- **参数**:
  - `fund_type` (必填): 基金类型
  - `start_date` (必填): 开始日期
  - `end_date` (必填): 结束日期
  - `interval` (必填): 分析间隔：month/quarter/year
  - `n` (必填): 每个周期筛选Top-N基金
  - `consistency_threshold` (必填): 一致性阈值（如 0.5 表示至少在 50% 的周期内进入 Top-N）
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "start_date": "2023-01-01",
      "end_date": "2024-01-01",
      "interval": "month",
      "n": 20,
      "consistency_threshold": 0.5,
      "list": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "fund_type": "混合型",
          "consistency_rate": 0.75,
          "top_n_count": 9,
          "total_periods": 12
        }
      ]
    }
  }
  ```

### 7.4 多因子评估
- **路径**: `/analysis/multifactor`
- **方法**: `GET`
- **参数**:
  - `fund_type` (必填): 基金类型
  - `start_date` (可选): 开始日期
  - `end_date` (可选): 结束日期
  - `t` (可选): 返回 Top-T 基金，默认20
  - `weight_config` (可选): 权重配置（JSON格式）
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "start_date": "2023-01-01",
      "end_date": "2024-01-01",
      "list": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "fund_type": "混合型",
          "total_score": 85.5,
          "return_score": 90.0,
          "risk_score": 75.0,
          "risk_adjusted_return_score": 88.0,
          "rank_score": 82.0,
          "rank": 1
        }
      ]
    }
  }
  ```

### 7.5 基金对比
- **路径**: `/analysis/compare`
- **方法**: `POST`
- **请求体**:
  ```json
  {
    "fund_codes": ["000001", "000002", "000003"],
    "start_date": "2023-01-01",
    "end_date": "2024-01-01"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "funds": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "fund_type": "混合型",
          "nav": 1.2345,
          "nav_growth_rate": 0.0123,
          "annual_return": 0.15,
          "max_drawdown": 0.2,
          "sharpe_ratio": 1.2
        },
        {
          "code": "000002",
          "name": "嘉实增长混合",
          "fund_type": "混合型",
          "nav": 1.5678,
          "nav_growth_rate": 0.0087,
          "annual_return": 0.12,
          "max_drawdown": 0.15,
          "sharpe_ratio": 1.0
        }
      ]
    }
  }
  ```

## 8. 自选基金API接口规范

### 8.1 获取自选基金列表
- **路径**: `/favorites`
- **方法**: `GET`
- **参数**:
  - `user_id` (可选): 用户ID
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "list": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "fund_type": "混合型",
          "nav": 1.2345,
          "nav_growth_rate": 0.0123,
          "add_time": "2024-01-01T10:00:00Z",
          "sort_order": 1,
          "note": "长期持有",
          "group_tag": "已持有"
        }
      ]
    }
  }
  ```

### 8.2 添加自选基金
- **路径**: `/favorites`
- **方法**: `POST`
- **请求体**:
  ```json
  {
    "fund_code": "000001",
    "user_id": "user123",
    "note": "长期持有",
    "group_tag": "已持有"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "添加成功"
    }
  }
  ```

### 8.3 删除自选基金
- **路径**: `/favorites/{code}`
- **方法**: `DELETE`
- **参数**:
  - `code` (必填): 基金代码
  - `user_id` (可选): 用户ID
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "删除成功"
    }
  }
  ```

### 8.4 更新自选基金排序
- **路径**: `/favorites/sort`
- **方法**: `PUT`
- **请求体**:
  ```json
  {
    "user_id": "user123",
    "funds": [
      {"code": "000001", "sort_order": 1},
      {"code": "000002", "sort_order": 2}
    ]
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "排序更新成功"
    }
  }
  ```

### 8.5 更新自选基金备注
- **路径**: `/favorites/{code}/note`
- **方法**: `PUT`
- **参数**:
  - `code` (必填): 基金代码
- **请求体**:
  ```json
  {
    "user_id": "user123",
    "note": "长期持有，目标收益率15%"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "备注更新成功"
    }
  }
  ```

### 8.6 获取自选基金分组
- **路径**: `/favorites/groups`
- **方法**: `GET`
- **参数**:
  - `user_id` (可选): 用户ID
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "groups": ["已持有", "观察中", "待买入"]
    }
  }
  ```

### 8.7 创建分组
- **路径**: `/favorites/groups`
- **方法**: `POST`
- **请求体**:
  ```json
  {
    "user_id": "user123",
    "group_name": "高风险"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "分组创建成功"
    }
  }
  ```

### 8.8 移动基金到分组
- **路径**: `/favorites/{code}/group`
- **方法**: `PUT`
- **参数**:
  - `code` (必填): 基金代码
- **请求体**:
  ```json
  {
    "user_id": "user123",
    "group_tag": "高风险"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "移动成功"
    }
  }
  ```

## 9. 自选基金评分API接口规范

### 9.1 获取自选基金评分
- **路径**: `/favorites/scores`
- **方法**: `GET`
- **参数**:
  - `user_id` (可选): 用户ID
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "list": [
        {
          "code": "000001",
          "name": "华夏成长混合",
          "score_date": "2024-03-15",
          "total_score": 85.5,
          "return_score": 90.0,
          "risk_score": 75.0,
          "risk_adjusted_return_score": 88.0,
          "rank_score": 82.0,
          "score_rank": 1,
          "score_change": 2.5,
          "score_trend": "上升"
        }
      ]
    }
  }
  ```

### 9.2 计算自选基金评分
- **路径**: `/favorites/scores/calculate`
- **方法**: `POST`
- **请求体**:
  ```json
  {
    "user_id": "user123"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "评分计算完成"
    }
  }
  ```

### 9.3 获取评分历史
- **路径**: `/favorites/scores/history`
- **方法**: `GET`
- **参数**:
  - `user_id` (可选): 用户ID
  - `fund_code` (必填): 基金代码
  - `days` (可选): 历史天数，默认30
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "code": "000001",
      "name": "华夏成长混合",
      "history": [
        {
          "score_date": "2024-03-15",
          "total_score": 85.5,
          "return_score": 90.0,
          "risk_score": 75.0
        },
        {
          "score_date": "2024-03-14",
          "total_score": 83.0,
          "return_score": 88.0,
          "risk_score": 74.0
        }
      ]
    }
  }
  ```

### 9.4 获取权重配置
- **路径**: `/favorites/scores/weights`
- **方法**: `GET`
- **参数**:
  - `user_id` (可选): 用户ID
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "weights": {
        "annual_return": 0.25,
        "cumulative_return": 0.1,
        "max_drawdown": 0.15,
        "volatility": 0.1,
        "downside_std": 0.05,
        "sharpe_ratio": 0.15,
        "sortino_ratio": 0.05,
        "calmar_ratio": 0.05,
        "rank_percentile": 0.1
      }
    }
  }
  ```

### 9.5 更新权重配置
- **路径**: `/favorites/scores/weights`
- **方法**: `PUT`
- **请求体**:
  ```json
  {
    "user_id": "user123",
    "weights": {
      "annual_return": 0.3,
      "cumulative_return": 0.1,
      "max_drawdown": 0.15,
      "volatility": 0.1,
      "downside_std": 0.05,
      "sharpe_ratio": 0.15,
      "sortino_ratio": 0.05,
      "calmar_ratio": 0.05,
      "rank_percentile": 0.1
    }
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "权重配置更新成功"
    }
  }
  ```

## 10. 自定义查询API接口规范

### 10.1 执行KQL查询
- **路径**: `/query/kql`
- **方法**: `POST`
- **请求体**:
  ```json
  {
    "query": "fund_basic_info | where fund_type == '混合型' | project code, name, fund_type | limit 10"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "columns": ["code", "name", "fund_type"],
      "rows": [
        ["000001", "华夏成长混合", "混合型"],
        ["000002", "嘉实增长混合", "混合型"]
      ]
    }
  }
  ```

### 10.2 获取查询历史
- **路径**: `/query/history`
- **方法**: `GET`
- **参数**:
  - `user_id` (可选): 用户ID
  - `limit` (可选): 限制条数，默认10
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "list": [
        {
          "query_id": "q123",
          "query": "fund_basic_info | where fund_type == '混合型' | limit 10",
          "executed_at": "2024-03-15T10:00:00Z"
        }
      ]
    }
  }
  ```

### 10.3 保存查询模板
- **路径**: `/query/templates`
- **方法**: `POST`
- **请求体**:
  ```json
  {
    "user_id": "user123",
    "name": "混合型基金查询",
    "query": "fund_basic_info | where fund_type == '混合型' | limit 10"
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "template_id": "t123",
      "name": "混合型基金查询",
      "success": true
    }
  }
  ```

### 10.4 获取查询模板
- **路径**: `/query/templates`
- **方法**: `GET`
- **参数**:
  - `user_id` (可选): 用户ID
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "list": [
        {
          "template_id": "t123",
          "name": "混合型基金查询",
          "query": "fund_basic_info | where fund_type == '混合型' | limit 10",
          "created_at": "2024-03-15T10:00:00Z"
        }
      ]
    }
  }
  ```

## 11. 系统API接口规范

### 11.1 获取基金类型列表
- **路径**: `/meta/fund-types`
- **方法**: `GET`
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "fund_types": ["股票型", "债券型", "混合型", "货币型", "指数型", "QDII", "FOF", "商品型", "其他"]
    }
  }
  ```

### 11.2 获取基金管理人列表
- **路径**: `/meta/managers`
- **方法**: `GET`
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "managers": ["华夏基金", "嘉实基金", "易方达基金", "南方基金"]
    }
  }
  ```

### 11.3 获取系统状态
- **路径**: `/system/status`
- **方法**: `GET`
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "system_status": "正常",
      "last_update_time": "2024-03-15T15:30:00Z",
      "fund_count": 10000,
      "db_size": "750MB",
      "update_status": "completed"
    }
  }
  ```

### 11.4 触发数据更新
- **路径**: `/system/update`
- **方法**: `POST`
- **请求体**:
  ```json
  {
    "update_type": "incremental" // incremental 或 full
  }
  ```
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "success": true,
      "message": "数据更新任务已启动",
      "task_id": "task123"
    }
  }
  ```

### 11.5 获取更新历史
- **路径**: `/system/update-history`
- **方法**: `GET`
- **参数**:
  - `limit` (可选): 限制条数，默认10
- **返回**:
  ```json
  {
    "code": 200,
    "message": "success",
    "data": {
      "list": [
        {
          "task_id": "task123",
          "update_type": "incremental",
          "start_time": "2024-03-15T15:30:00Z",
          "end_time": "2024-03-15T15:45:00Z",
          "status": "completed",
          "records_updated": 10000
        }
      ]
    }
  }
  ```

## 12. API文档访问
- **路径**: `/swagger`
- **方法**: `GET`
- **描述**: 访问Swagger UI文档

## 13. API版本管理
- **当前版本**: v1
- **版本控制**: 通过路径前缀 `/api/v1` 进行版本控制

## 14. 认证方式
- **JWT认证**: 用于需要用户身份的接口
- **API Key**: 用于系统管理接口

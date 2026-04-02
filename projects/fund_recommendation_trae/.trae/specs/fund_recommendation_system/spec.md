# 基金信息管理分析系统 - 产品需求文档

## Overview
- **Summary**: 构建一个集基金数据采集、存储、量化分析和推荐于一体的基金信息管理系统，帮助投资者筛选优质基金。
- **Purpose**: 为个人投资者、基金分析师和理财顾问提供全面的基金数据和分析工具，帮助他们做出更明智的投资决策。
- **Target Users**: 个人投资者、基金分析师、理财顾问

## Goals
- 实现基金数据自动采集与更新（从akshare数据源）
- 提供多维度基金业绩分析功能
- 实现智能基金推荐系统
- 支持自定义数据查询（类KQL查询语言）
- 提供直观的用户界面和数据可视化

## Non-Goals (Out of Scope)
- 不支持实时交易功能
- 不提供投资组合管理功能
- 不包含基金评级机构的外部评级数据
- 不支持多用户系统（V1版本）
- 不实现复杂的机器学习模型（V1版本）

## Background & Context
- 基金市场数据量大，手工分析效率低
- 投资者需要专业工具辅助决策
- akshare提供了丰富的基金数据API
- 现有工具要么功能简单，要么价格昂贵
- 系统采用前后端分离架构，技术栈成熟稳定

## System Architecture

### Overall Architecture
```
┌───────────────────────────────────────────────────────────────────────────┐
│                              前端展示层                                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐         │
│  │  基金列表页  │  │  基金详情页  │  │ 多因子评估页 │  │  自选基金页  │         │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘         │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐                         │
│  │  基金对比页  │  │ 自定义查询页 │  │  数据管理页  │                         │
│  └────────────┘  └────────────┘  └────────────┘                         │
└─────────────────────────────┬─────────────────────────────────────────────┘
                              │ HTTP/REST API
┌─────────────────────────────▼─────────────────────────────────────────────┐
│                              后端服务层                                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐         │
│  │  基金查询   │  │  分析计算   │  │  KQL查询   │  │  自选基金   │         │
│  │  服务      │  │  服务      │  │  服务      │  │  服务      │         │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘         │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐                         │
│  │  数据采集   │  │  数据更新   │  │  定时任务   │                         │
│  │  服务      │  │  服务      │  │  调度      │                         │
│  └────────────┘  └────────────┘  └────────────┘                         │
└─────────────────────────────┬─────────────────────────────────────────────┘
                              │ SQL/SQLite
┌─────────────────────────────▼─────────────────────────────────────────────┐
│                              数据存储层                                   │
│                           SQLite 数据库                                   │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐         │
│  │ fund_basic_info   │ │ fund_nav_history │ │ fund_performance  │         │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘         │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐         │
│  │ fund_manager      │ │ fund_corporate_  │ │ fund_asset_scale  │         │
│  │                   │ │ actions          │ │                   │         │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘         │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐         │
│  │ user_favorite_    │ │ user_favorite_   │ │ fund_purchase_    │         │
│  │ funds             │ │ scores           │ │ status           │         │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘         │
│  ┌──────────────────┐ ┌──────────────────┐                              │
│  │ fund_redemption_  │ │ fund_scale_      │                              │
│  │ status            │ │ history          │                              │
│  └──────────────────┘ └──────────────────┘                              │
└───────────────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────▼─────────────────────────────────────────────┐
│                            外部数据源                                     │
│                            akshare API                                    │
└───────────────────────────────────────────────────────────────────────────┘
```

### Technology Stack
| 层级 | 技术选型 | 说明 |
|------|----------|------|
| 前端 | Vue 3 + Element Plus + ECharts | 用户界面展示 |
| 后端 | .NET 8 Web API | REST API服务 |
| 数据库 | SQLite 3.x | 数据持久化存储 |
| 数据源 | akshare（Python脚本） | 基金数据获取 |
| 任务调度 | .NET BackgroundService | 定时数据更新 |

## Database Design

### Database Tables

#### 1. 基金基本信息表 (fund_basic_info)
- **Primary Key**: code (VARCHAR(20))
- **Fields**:
  - code: VARCHAR(20) - 基金唯一标识代码
  - name: VARCHAR(100) - 基金全称
  - fund_type: VARCHAR(20) - 基金类型
  - share_type: VARCHAR(10) - 份额类型
  - main_fund_code: VARCHAR(20) - 主基金代码
  - establish_date: DATE - 成立日期
  - list_date: DATE - 上市日期
  - manager: VARCHAR(100) - 基金管理人
  - custodian: VARCHAR(100) - 基金托管人
  - management_fee_rate: DECIMAL(5,4) - 管理费率
  - custodian_fee_rate: DECIMAL(5,4) - 托管费率
  - sales_fee_rate: DECIMAL(5,4) - 销售服务费率
  - benchmark: VARCHAR(500) - 业绩比较基准
  - tracking_target: VARCHAR(100) - 跟踪标的
  - investment_style: VARCHAR(20) - 投资风格
  - risk_level: VARCHAR(10) - 风险等级
  - update_time: DATETIME - 更新时间

#### 2. 基金资产规模表 (fund_asset_scale)
- **Primary Key**: (code, date)
- **Fields**:
  - code: VARCHAR(20) - 基金代码
  - date: DATE - 规模统计日期
  - asset_scale: DECIMAL(12,2) - 资产规模(亿元)
  - share_scale: DECIMAL(12,2) - 份额规模(亿份)
  - update_time: DATETIME - 更新时间

#### 3. 基金经理表 (fund_manager)
- **Primary Key**: (code, manager_name, start_date)
- **Fields**:
  - code: VARCHAR(20) - 基金代码
  - manager_name: VARCHAR(50) - 基金经理姓名
  - start_date: DATE - 任职开始日期
  - end_date: DATE - 任职结束日期
  - manage_days: INT - 管理天数
  - update_time: DATETIME - 更新时间

#### 4. 基金申购状态表 (fund_purchase_status)
- **Primary Key**: (code, date)
- **Fields**:
  - code: VARCHAR(20) - 基金代码
  - date: DATE - 状态记录日期
  - purchase_status: VARCHAR(20) - 申购状态
  - purchase_limit: DECIMAL(12,2) - 申购限额(元)
  - purchase_fee_rate: DECIMAL(5,4) - 申购手续费率
  - update_time: DATETIME - 更新时间

#### 5. 基金赎回状态表 (fund_redemption_status)
- **Primary Key**: (code, date)
- **Fields**:
  - code: VARCHAR(20) - 基金代码
  - date: DATE - 状态记录日期
  - redemption_status: VARCHAR(20) - 赎回状态
  - redemption_limit: DECIMAL(12,2) - 赎回限额(元)
  - redemption_fee_rate: DECIMAL(5,4) - 赎回手续费率
  - update_time: DATETIME - 更新时间

#### 6. 基金历史净值表 (fund_nav_history)
- **Primary Key**: (code, date)
- **Fields**:
  - code: VARCHAR(20) - 基金代码
  - date: DATE - 净值日期
  - nav: DECIMAL(10,4) - 单位净值
  - accumulated_nav: DECIMAL(10,4) - 累计净值
  - daily_growth_rate: DECIMAL(8,4) - 日增长率(%)
  - update_time: DATETIME - 更新时间

#### 7. 基金公司行为表 (fund_corporate_actions)
- **Primary Key**: id (INTEGER, AUTOINCREMENT)
- **Unique Index**: (code, ex_date, event_type)
- **Fields**:
  - id: INTEGER - 自增主键
  - code: VARCHAR(20) - 基金代码
  - ex_date: DATE - 除权日期
  - event_type: VARCHAR(20) - 事件类型
  - dividend_per_share: DECIMAL(10,4) - 每份分红金额
  - payment_date: DATE - 分红发放日
  - split_ratio: DECIMAL(10,4) - 拆分比例
  - record_date: DATE - 权益登记日
  - event_description: VARCHAR(500) - 事件详细描述
  - announcement_date: DATE - 公告日期
  - update_time: DATETIME - 更新时间

#### 8. 基金业绩表 (fund_performance)
- **Primary Key**: id (INTEGER, AUTOINCREMENT)
- **Unique Index**: (code, period_type, period_value)
- **Fields**:
  - id: INTEGER - 自增主键
  - code: VARCHAR(20) - 基金代码
  - period_type: VARCHAR(20) - 周期类型
  - period_value: VARCHAR(20) - 周期值
  - nav_growth_rate: DECIMAL(8,4) - 净值增长率(%)
  - max_drawdown: DECIMAL(8,4) - 最大回撤(%)
  - downside_std: DECIMAL(8,4) - 下行标准差
  - sharpe_ratio: DECIMAL(8,4) - 夏普比率
  - sortino_ratio: DECIMAL(8,4) - 索提诺比率
  - calmar_ratio: DECIMAL(8,4) - 卡玛比率
  - annual_return: DECIMAL(8,4) - 年化收益率(%)
  - volatility: DECIMAL(8,4) - 波动率(%)
  - rank_in_category: INT - 同类型基金排名
  - total_in_category: INT - 同类型基金总数
  - update_time: DATETIME - 更新时间

#### 9. 用户自选基金表 (user_favorite_funds)
- **Primary Key**: id (INTEGER, AUTOINCREMENT)
- **Unique Index**: (user_id, fund_code)
- **Fields**:
  - id: INTEGER - 自增主键
  - user_id: VARCHAR(50) - 用户ID
  - fund_code: VARCHAR(20) - 基金代码
  - add_time: DATETIME - 添加时间
  - sort_order: INT - 排序序号
  - note: VARCHAR(500) - 备注
  - group_tag: VARCHAR(50) - 分组标签
  - alert_settings: VARCHAR(200) - 提醒设置(JSON)
  - update_time: DATETIME - 更新时间

#### 10. 自选基金多因子评分表 (user_favorite_scores)
- **Primary Key**: id (INTEGER, AUTOINCREMENT)
- **Unique Index**: (user_id, fund_code, score_date)
- **Fields**:
  - id: INTEGER - 自增主键
  - user_id: VARCHAR(50) - 用户ID
  - fund_code: VARCHAR(20) - 基金代码
  - score_date: DATE - 评分日期
  - total_score: DECIMAL(5,2) - 综合评分
  - return_score: DECIMAL(5,2) - 收益因子得分
  - risk_score: DECIMAL(5,2) - 风险因子得分
  - risk_adjusted_return_score: DECIMAL(5,2) - 风险调整收益得分
  - rank_score: DECIMAL(5,2) - 排名因子得分
  - score_rank: INT - 评分排名
  - score_change: DECIMAL(5,2) - 评分变化
  - score_trend: VARCHAR(10) - 评分趋势
  - weight_config: VARCHAR(500) - 权重配置(JSON)
  - calculate_time: DATETIME - 评分计算时间
  - update_time: DATETIME - 更新时间

## Functional Requirements
- **FR-1**: 基金数据采集与更新
  - 从akshare API获取基金数据
  - 支持手动触发和定时调度
  - 支持增量更新和全量更新
  - 数据清洗和校验
  - 确保所有数据库表的数据更新
  - 处理akshare API限流问题
  - 实现数据更新并发控制

- **FR-2**: 基金基础信息管理
  - 存储和管理基金基本信息
  - 支持按基金类型、管理人等筛选
  - 提供基金详情查询

- **FR-3**: 基金历史净值管理
  - 存储基金历史净值数据
  - 支持净值走势查询
  - 计算复权净值
  - 处理复权净值的动态变化特性

- **FR-4**: 基金业绩分析
  - 单周期排名筛选
  - 周期变化率排名筛选
  - 多周期一致性筛选
  - 多因子量化评估
  - 支持自定义因子权重

- **FR-5**: 自选基金管理
  - 添加/移除自选基金
  - 分组管理
  - 排序管理
  - 备注和提醒设置
  - 自选基金多因子评分

- **FR-6**: 基金对比分析
  - 支持多只基金同时对比
  - 提供基本信息、业绩指标对比
  - 净值走势对比图
  - 多因子雷达图对比

- **FR-7**: 自定义查询
  - 支持类KQL查询语言
  - 支持基础查询操作：where、project、limit
  - 查询结果可视化
  - 实现查询安全限制

- **FR-8**: 系统管理
  - 数据备份与恢复
  - 系统状态监控
  - 数据更新历史查询
  - 集成测试
  - 端到端测试
  - 日志管理

- **FR-9**: 数据库完整性管理
  - 确保所有数据库表的创建和维护
  - 保证表结构与需求文档一致
  - 确保字段定义、索引设计符合要求
  - 维护数据一致性和完整性

## Non-Functional Requirements
- **NFR-1**: 性能要求
  - 基金列表加载时间 < 2秒
  - 单次分析计算时间 < 10秒
  - 数据更新时间（增量） < 15分钟
  - 数据更新时间（全量） < 60分钟
  - 并发用户数 >= 10

- **NFR-2**: 安全要求
  - 数据库文件加密存储
  - API接口限流（100次/分钟）
  - 敏感配置使用环境变量
  - SQL注入防护
  - XSS防护
  - CSRF防护
  - KQL查询安全限制

- **NFR-3**: 可靠性要求
  - 系统可用性 > 99%
  - 数据备份机制（每日自动备份，保留7天）
  - 异常处理和容错机制
  - 任务失败重试机制
  - 数据更新并发控制

- **NFR-4**: 可维护性要求
  - 代码覆盖率 >= 70%
  - 日志级别可配置
  - 配置文件外置
  - 每个代码模块都有单元测试
  - 编译通过，没有错误和警告
  - 代码结构清晰，模块化设计

- **NFR-5**: 兼容性要求
  - 浏览器：Chrome 90+, Edge 90+, Firefox 88+
  - 屏幕分辨率：最低 1366x768

- **NFR-6**: 数据质量要求
  - 数据完整性 >= 99.9%
  - 数据准确性验证机制
  - 数据及时性监控
  - 异常数据处理流程

- **NFR-7**: 监控与告警要求
  - 系统运行状态监控
  - 数据更新状态监控
  - 性能指标监控
  - 异常告警机制
  - 结构化日志记录

## Constraints
- **Technical**: 
  - 后端：.NET 8 Web API
  - 前端：Vue 3 + Element Plus + ECharts
  - 数据库：SQLite 3.x
  - 数据采集：Python 3.11+ + akshare
  - 任务调度：.NET BackgroundService
  - 缓存：多级缓存架构

- **Business**: 
  - 预算有限，优先实现核心功能
  - 开发周期：3-4个月
  - 优先保证数据准确性和系统稳定性

- **Dependencies**: 
  - akshare API稳定性
  - .NET 8 SDK
  - Node.js 18+ LTS
  - Python 3.11+
  - SQLite 3.x
  - Element Plus 2.x
  - ECharts 5.x

## Data Storage Strategy
- **基金基本信息**：永久存储，实时更新
- **历史净值数据**：保留10年，增量追加，定期归档
- **基金业绩数据**：保留10年，定期计算，覆盖更新
- **基金规模数据**：保留10年，增量追加，定期归档
- **基金经理任职记录**：永久存储，增量追加
- **申赎状态记录**：保留10年，增量追加，定期清理
- **用户自选基金**：永久存储，实时更新
- **自选基金评分**：保留1年，增量追加，定期清理

## Cache Strategy
- **内存缓存**：热点数据，1小时TTL
- **本地缓存**：历史净值、业绩数据，24小时TTL
- **应用层缓存**：API响应，10分钟TTL
- **缓存更新机制**：数据更新时主动失效，热点数据定时刷新
- **缓存一致性**：使用缓存版本号，避免脏读

## Deployment Strategy
- **开发环境**：本地开发，使用.NET和Node.js开发服务器
- **测试环境**：Docker容器化部署，集成测试和端到端测试
- **生产环境**：Docker容器化部署，Nginx反向代理
- **数据备份**：每日自动备份，保留7天的每日备份和每月1号的月备份
- **监控**：系统运行状态监控，数据更新状态监控，性能指标监控

## Monitoring & Maintenance
- **日志系统**：结构化JSON格式日志，支持多级别日志
- **监控指标**：CPU使用率、内存使用率、磁盘使用率、API响应时间、数据更新延迟
- **告警机制**：阈值告警，异常通知
- **常见问题处理**：数据更新失败、API响应缓慢、前端页面加载缓慢、数据库文件过大

## Development Plan
- **阶段一**：基础架构搭建（2周）
- **阶段二**：数据采集模块（3周）
- **阶段三**：核心分析模块（4周）
- **阶段四**：前端开发（4周）
- **阶段五**：集成测试（2周）
- **阶段六**：部署上线（1周）

## Risk Assessment
- **技术风险**：akshare接口不稳定、SQLite性能瓶颈、数据量增长过快、并发访问冲突
- **业务风险**：基金数据准确性、市场环境变化、用户需求变更、合规性要求
- **实施风险**：开发资源不足、技术债务积累、测试覆盖不足、部署环境问题

## Assumptions
- akshare API提供稳定的数据接口
- 系统运行环境具备足够的网络带宽
- 用户具备基本的基金投资知识
- 数据更新期间系统可以接受短暂的服务降级
- 基金数据量在可预见的未来不会超过SQLite的处理能力

## Acceptance Criteria

### AC-1: 数据采集功能
- **Given**: 系统已配置akshare API
- **When**: 触发数据更新任务
- **Then**: 系统能够从akshare获取数据并存储到数据库
- **Verification**: `programmatic`
- **Notes**: 验证数据更新日志和数据库记录

### AC-2: 基金列表查询
- **Given**: 系统已存储基金数据
- **When**: 用户访问基金列表页
- **Then**: 系统显示基金列表，支持筛选和排序
- **Verification**: `human-judgment`
- **Notes**: 验证页面响应速度和数据准确性

### AC-3: 基金详情查询
- **Given**: 系统已存储基金数据
- **When**: 用户点击基金列表中的基金
- **Then**: 系统显示基金详情，包括基本信息、净值走势、业绩指标
- **Verification**: `human-judgment`
- **Notes**: 验证数据完整性和图表显示

### AC-4: 多因子评估
- **Given**: 系统已存储基金数据
- **When**: 用户访问多因子评估页
- **Then**: 系统显示多因子评分结果和推荐基金
- **Verification**: `programmatic`
- **Notes**: 验证评分计算准确性

### AC-5: 自选基金管理
- **Given**: 用户已登录系统
- **When**: 用户添加基金到自选
- **Then**: 系统将基金添加到自选列表，并计算多因子评分
- **Verification**: `programmatic`
- **Notes**: 验证数据库记录和评分计算

### AC-6: 基金对比分析
- **Given**: 系统已存储基金数据
- **When**: 用户选择多只基金进行对比
- **Then**: 系统显示基金对比结果，包括基本信息、业绩指标、净值走势对比
- **Verification**: `human-judgment`
- **Notes**: 验证对比数据准确性和图表显示

### AC-7: 自定义查询
- **Given**: 系统已存储基金数据
- **When**: 用户在自定义查询页输入KQL查询
- **Then**: 系统执行查询并显示结果
- **Verification**: `programmatic`
- **Notes**: 验证查询执行和结果准确性

### AC-8: 系统管理
- **Given**: 系统已运行
- **When**: 管理员访问系统设置页
- **Then**: 系统显示系统状态，支持数据备份和恢复
- **Verification**: `human-judgment`
- **Notes**: 验证系统状态显示和备份功能

## Open Questions
- [ ] akshare API的稳定性和数据质量如何保证？
- [ ] 系统如何处理akshare API限流问题？
- [ ] 当数据量增长到一定程度时，如何优化SQLite性能？
- [ ] 如何确保复权净值计算的准确性？
- [ ] 系统如何处理基金经理变更等特殊情况？
- [ ] 如何实现查询安全限制，防止SQL注入？
- [ ] 如何优化系统性能，满足性能要求？
- [ ] 如何确保系统在数据更新期间的可用性？
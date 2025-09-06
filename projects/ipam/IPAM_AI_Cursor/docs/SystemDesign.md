# IPAM 系统设计文档

## 概述
- **目标**: 使用 C# 和 .NET (ASP.NET Core) 构建企业级 IP 地址管理系统 (IPAM)，满足高可用、高性能、可扩展以及高测试覆盖率要求。
- **范围**: 支持 Address Space、IP (CIDR) 与 Tag 的 CRUD；RBAC 授权；限流、黑白名单；REST API、Web Portal、.NET SDK、PowerShell CLI；Azure Table Storage 持久化；容器化与 K8s/Azure Container Apps 部署；全链路遥测与健康监控。

## 领域模型
### 核心实体
- **AddressSpace**
  - `Id` (Guid)
  - `Name` (string)
  - `Description` (string?)
  - `CreatedOn` (DateTimeOffset)
  - `ModifiedOn` (DateTimeOffset)
  - 语义: IPv6 `::/0` 根的树中某个逻辑命名空间，包含 IPv4/IPv6 CIDR 树。
- **IpCidr**
  - `AddressSpaceId` (Guid)
  - `Id` (Guid)
  - `Prefix` (string, CIDR 表达式，如 `10.0.0.0/16`, `2001:db8::/32`)
  - `Tags` (List<TagAssignment>)
  - `CreatedOn`, `ModifiedOn` (DateTimeOffset)
  - `ParentId` (Guid?)
  - `ChildrenIds` (List<Guid>)
  - 约束: 子节点 CIDR 可以与父节点相同，但子节点必须比父节点多至少一个可继承 Tag。
- **TagDefinition**
  - `AddressSpaceId` (Guid)
  - `Name` (string, 唯一)
  - `Description` (string?)
  - `Type` (enum: Inheritable | NonInheritable)
  - `KnownValues` (List<string>?)
  - `Attributes` (Dictionary<string, Dictionary<string,string>>)
  - `Implications` (Dictionary<string, List<(string tagName, string value)>>) // value-> implied pairs
  - `CreatedOn`, `ModifiedOn`
- **TagAssignment**
  - `Name` (string)
  - `Value` (string)
  - 派生/继承标识: `IsInherited` (bool)

### 领域规则
- **继承与冲突**
  - Inheritable Tag 会由父节点下沉到子节点；父子间同名可继承 Tag 值不能冲突。
  - 父节点删除时，其可继承 Tags 下沉到所有子节点（物化）。
- **KnownValues 约束**
  - 若定义了 `KnownValues`，赋值必须来自集合；否则可任意值。
- **Imply 推断**
  - 例如: `Datacenter=AMS05 => Region=EuropeWest`；推断可传递闭包。
  - 在添加/修改 Tag 时，自动闭包推断补全缺失的派生 Tag。
- **子节点 Tag 增量**
  - 子节点的 Inheritable Tags 必须至少比父节点多一个键或更细化的值。

## API 与用例
### Address Space
- CRUD；查询条件：`Id` 精确、`Name` 关键字、`CreatedOn` 时间范围。
### Tag Definition
- CRUD；查询条件：`AddressSpaceId+Name` 精确、`AddressSpaceId+Name` 关键字。
### IP (CIDR)
- CRUD（包含 Tag 的增删改）；查询条件：
  - `AddressSpaceId + IP Id`
  - `AddressSpaceId + CIDR`
  - `AddressSpaceId + Tags (AND 组合)`
  - `AddressSpaceId + ParentId` 获取子节点

## 架构
### 总体
- **微服务**
  - API Gateway: 路由、认证、鉴权、限流、黑白名单（基于 YARP/Envoy 可选）。
  - Frontend 服务: 提供 IPAM 领域 API（REST）。
  - DataAccess 服务: 封装 Azure Table Storage 访问，聚焦存储、分区策略、并发控制。
- **客户端**: Web Portal (ASP.NET Core MVC + Bootstrap + jQuery), .NET SDK, PowerShell CLI。

### 依赖注入与接口隔离
- 按层定义接口：`Domain.Abstractions`、`Application.Abstractions`、`Infrastructure.Abstractions`。
- 每层仅依赖下层的抽象，不依赖实现。

### 存储设计（Azure Table Storage）
- 表划分：
  - `AddressSpaces`
  - `Tags`
  - `IpCidrs`
- 分区策略：
  - 每个 Address Space 仅属于一个 Partition；PartitionKey 可设为 `AS:{AddressSpaceId}`。
  - RowKey：
    - AddressSpace: `AS:{Id}`
    - Tag: `TAG:{Name}`
    - IpCidr: `IP:{Id}` 或 `IP:{Prefix}:{Id}`（支持前缀检索）
- 索引与查询：
  - 关键检索 (Id/Name/CIDR/Tag) 通过冗余存储+二级表实现，如 `IpCidrsByPrefix`、`IpCidrsByTagValue`。
- 并发：使用 ETag 乐观并发控制。

### 树结构与层级维护
- 通过字段 `ParentId` 与 `ChildrenIds` 维护拓扑；
- 祖先可继承 Tag 通过计算视图获得；删除父节点时代入子节点（批量事务分片）。

### 授权与安全
- 身份认证：OpenID Connect/OAuth2（Azure AD, Entra ID）。
- 授权：RBAC（SystemAdmin、AddressSpaceAdmin、AddressSpaceViewer）。
- 限流：令牌桶或固定窗口（API Gateway 层）。
- 黑白名单：CIDR/IP 清单在网关校验。

### 可观测性
- 日志: Serilog + Azure Monitor/Log Analytics。
- 追踪: OpenTelemetry (OTLP) + Azure Monitor。
- 指标: Prometheus 格式导出，接入 Azure Managed Prometheus。
- 健康检查: ASP.NET Core Health Checks + 存储/依赖检查。

### 性能与扩展性
- 读多写少：读路径引入缓存（分布式 Cache，如 Redis）。
- 批处理：父节点删除 Tag 下沉使用后台任务与批量操作。
- 分区横向扩展：Address Space 数量增加时按 Partition 扩容。

## 接口契约 (REST 摘要)
- 路径前缀：`/api/v1`
- AddressSpaces
  - `GET /address-spaces`（查询）
  - `POST /address-spaces`（创建）
  - `GET /address-spaces/{id}`（详情）
  - `PUT /address-spaces/{id}`（更新）
  - `DELETE /address-spaces/{id}`（删除）
- Tags
  - `GET /address-spaces/{asId}/tags`
  - `POST /address-spaces/{asId}/tags`
  - `GET /address-spaces/{asId}/tags/{name}`
  - `PUT /address-spaces/{asId}/tags/{name}`
  - `DELETE /address-spaces/{asId}/tags/{name}`
- IPs
  - `GET /address-spaces/{asId}/ips`
  - `POST /address-spaces/{asId}/ips`
  - `GET /address-spaces/{asId}/ips/{ipId}`
  - `PUT /address-spaces/{asId}/ips/{ipId}`
  - `DELETE /address-spaces/{asId}/ips/{ipId}`
  - `GET /address-spaces/{asId}/ips/by-cidr/{cidr}`
  - `GET /address-spaces/{asId}/ips/{ipId}/children`

## 技术选型
- .NET 8, C# 12
- ASP.NET Core (Minimal APIs + MVC for Portal)
- YARP 反向代理
- Azure.Data.Tables SDK
- Identity (OpenIdConnect + JWT Bearer)
- Serilog, OpenTelemetry
- xUnit + FluentAssertions + NSubstitute
- Docker, Helm/K8s, Azure Container Apps

## 关键算法与校验
- **CIDR 校验与关系**: 解析、规范化、父子判定、相等判定。
- **Tag 冲突检测**: 合并祖先可继承 Tag，检测同名值冲突。
- **Imply 推断闭包**: 有向图的传递闭包；循环检测与去重。
- **子节点增量校验**: 子节点的 Inheritable Tag 集至少严格包含父节点集合。

## 失败与恢复
- 幂等性：写操作提供请求 Id；网关与前端重试策略。
- 备份与恢复：定期导出表数据至 Blob；提供导入工具。

## 部署
- 开发：Docker Compose 启动各服务 + Azurite (Table)
- 生产：K8s/ACA，多副本，HPA，蓝绿或金丝雀发布。

## 目录与项目结构（拟）
- `src/`
  - `IPAM.sln`
  - `Services/Gateway` (YARP)
  - `Services/Frontend` (REST API)
  - `Services/DataAccess` (Table SDK 封装)
  - `Web/WebPortal` (MVC)
  - `Clients/SDK` (.NET SDK)
  - `Clients/CLI` (.NET Console)
  - `Clients/PowerShell` (PS 模块)
  - `Shared/Domain` (实体与逻辑)
  - `Shared/Application` (用例/服务接口)
  - `Shared/Infrastructure` (实现)
  - `Shared/Contracts` (DTO)
- `tests/` 各项目对应测试
- `deploy/` Docker 与 K8s/Helm 清单

## 非功能性需求
- 覆盖率 ≥ 90%，强制 CI 质量门槛
- SLA/可用性：多实例、无状态服务 + 存储冗余
- 安全：最小权限、密钥管理、审计日志

## 风险与缓解
- Table Storage 二级索引复杂：通过冗余索引表与异步一致性缓解。
- Tag 推断循环：在写路径校验环路并拒绝。
- 大规模批处理：分片 + 重试 + 死信队列。

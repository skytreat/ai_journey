# IPAM System Design Document

## 概述

本系统设计文档描述企业级IP地址管理系统（IPAM）的技术架构、模块划分、数据模型、服务接口、部署架构以及测试方案。

## 架构概览

- **微服务架构**：采用API网关、前端服务、数据访问服务的分布式架构，支持高可用、高性能及横向扩展。
- **持久化存储**：利用Azure Table Storage存储Address Space、IP及Tag数据，每个Address Space对应单一分区。
- **安全设计**：支持用户认证、授权（RBAC）、限流、黑白名单；不同角色（SystemAdmin, AddressSpaceAdmin, AddressSpaceViewer）具有不同权限。
- **数据模型**：
  - IP数据模型：Id, Prefix, Tags, CreatedOn, ModifiedOn, ParentId
  - Tag数据模型：Name, Description, CreatedOn, ModifiedOn, Type, KnownValues
- **继承规则**：Inheritable Tag支持子节点继承，但必须保证标签值无冲突；父节点删除时，标签下沉至子节点。
- **接口设计**：提供RESTful API进行增删改查操作；Web Portal、C#客户端与PowerShell CLI均基于该API。
- **容器化与部署**：支持Docker容器、K8s及Azure Container Apps部署。
- **监控与日志**：采用Azure友好的Telemetry系统进行日志记录与系统监控。
- **测试**：具备全面的单元测试、集成测试和功能测试，整体代码覆盖率不低于90%。

## 模块划分

1. **API网关服务**
   - 职责：请求路由、用户认证、权限鉴定、限流与黑白名单管理。
2. **前端服务**
   - 职责：提供AddressSpace、IP和Tag的CRUD RESTful API接口。
3. **数据访问服务**
   - 职责：与Azure Table Storage交互，实现数据存取。
4. **Web Portal**
   - 基于ASP.NET Core MVC和Bootstrap实现用户界面。
5. **C#客户端**
   - 通过调用RESTful API实现操作。
6. **PowerShell CLI**
   - 基于C#客户端实现命令行交互。

## 技术选型

- 开发语言：C#
- 框架：.NET 6/7 (Minimal Hosting)
- 数据存储：Azure Table Storage
- 部署：Docker, K8s, Azure Container Apps

## 安全与权限

- 采用JWT及OAuth 2.0进行用户认证
- 支持RBAC，不同角色拥有不同访问策略

## 测试策略

- 单元测试：覆盖核心业务逻辑
- 集成测试：验证各微服务间接口交互
- 功能测试：确保关键业务流程正常运行

## 部署方案

- 容器化部署：使用Docker镜像
- 集群管理：利用Kubernetes实现高可用部署

## 日志与监控

- 使用Azure Telemetry系统进行日志收集、错误追踪及系统健康监控告警

## 详细模块设计

### API网关服务
- 负责请求路由和集中认证、授权、安全检查
- 实现基于JWT的安全认证机制
- 限流策略：基于IP和用户角色

### 前端服务
- 提供RESTful API接口
- 实现增删改查操作的业务逻辑

### 数据访问服务
- 封装Azure Table Storage操作
- 实现数据分区策略（多个AddressSpace对应一个分区）
- 遵循接口隔离与依赖注入原则

### Web Portal
- ASP.NET Core MVC实现，UI采用Bootstrap
- 支持地址查询、细粒度管理及批量操作

### C#客户端和PowerShell CLI
- 调用RESTful API接口
- 提供命令行参数解析与日志记录

## 服务交互流程

1. 用户通过Web Portal或C#客户端发起服务请求
2. API网关进行认证、鉴权及限流控制
3. 请求转发到前端服务进行业务处理
4. 数据访问服务负责实际数据存储和查询
5. 日志通过Telemetry系统集中收集与分析

## 安全设计详细说明

- 用户认证：基于JWT和OAuth2.0
- 角色授权：RBAC实现精细权限控制
- 数据校验：包括IP CIDR格式校验和Tag继承规则检查
- 全链路日志与监控，集中收集服务运行数据

## 部署与运维

- 使用Docker构建各服务镜像
- 利用K8s实现服务编排与滚动更新
- 使用Azure Container Apps实现弹性伸缩
- 集成CI/CD流水线实现自动化部署

## 测试及质量保证

- 单元测试覆盖关键业务逻辑
- 集成测试验证服务接口交互
- 功能测试模拟真实业务场景
- 静态代码分析工具确保代码质量

## 数据模型设计

### 数据库表设计
使用Azure Table Storage存储数据，主要包含以下表：

1. AddressSpaceTable
   - PartitionKey: PartitionId
   - RowKey: AddressSpaceId
   - Properties: 
     - Name: string
     - Description: string
     - CreatedOn: DateTime
     - ModifiedOn: DateTime
     - Status: string

2. TagTable
   - PartitionKey: AddressSpaceId
   - RowKey: TagName
   - Properties:
     - Description: string
     - Type: string (Inheritable/NonInheritable)
     - CreatedOn: DateTime
     - ModifiedOn: DateTime
     - KnownValues: string[] (JSON)
     - Implies: Dictionary<string, Dictionary<string, string>> (JSON)
     - Attributes: Dictionary<string, Dictionary<string, string>> (JSON)

3. IPTable
   - PartitionKey: AddressSpaceId
   - RowKey: IPId
   - Properties:
     - Prefix: string (CIDR格式)
     - ParentId: string
     - ChildrenIds: string[] (JSON)
     - Tags: Dictionary<string, string> (JSON)
     - CreatedOn: DateTime
     - ModifiedOn: DateTime

### 数据关系
```
AddressSpace(1) ----< Tag(n)
AddressSpace(1) ----< IP(n)
IP(1) ----< IP(n) [Parent-Child关系]
```

### 并发控制
1. 乐观并发控制
   - 使用ETag进行乐观锁控制
   - 在更新操作时检查ETag值

2. 事务处理
   - IP节点操作的原子性保证
   - Tag继承关系的一致性维护

## 详细实现方案

### IP树操作实现
1. IP节点创建流程
```csharp
// 示例伪代码
public async Task<IPNode> CreateIPNode(string addressSpaceId, string cidr, Dictionary<string, string> tags)
{
    // 1. 验证CIDR格式
    ValidateCIDR(cidr);
    
    // 2. 查找父节点
    var parentNode = await FindParentNode(addressSpaceId, cidr);
    
    // 3. 验证Tag继承
    ValidateTagInheritance(parentNode?.Tags, tags);
    
    // 4. 创建IP节点
    var ipNode = new IPNode {
        AddressSpaceId = addressSpaceId,
        Prefix = cidr,
        ParentId = parentNode?.Id,
        Tags = tags
    };
    
    // 5. 更新父子关系
    await UpdateParentChildRelation(ipNode, parentNode);
    
    return ipNode;
}
```

### Tag继承实现
```csharp
// 示例伪代码
private async Task ValidateTagInheritance(Dictionary<string, string> parentTags, Dictionary<string, string> newTags)
{
    foreach (var tag in await GetInheritableTags())
    {
        if (parentTags.ContainsKey(tag.Name) && newTags.ContainsKey(tag.Name))
        {
            if (parentTags[tag.Name] != newTags[tag.Name])
            {
                throw new TagConflictException($"Tag {tag.Name} value conflicts with parent");
            }
        }
    }
}
```

### 标签推断实现
```csharp
// 示例伪代码
private async Task<Dictionary<string, string>> InferTags(Dictionary<string, string> originalTags)
{
    var result = new Dictionary<string, string>(originalTags);
    var processedTags = new HashSet<string>();
    
    while (true)
    {
        var inferredTags = await InferTagsOneLevel(result, processedTags);
        if (inferredTags.Count == 0) break;
        
        foreach (var tag in inferredTags)
        {
            result[tag.Key] = tag.Value;
        }
        processedTags.UnionWith(inferredTags.Keys);
    }
    
    return result;
}
```

## API接口详细设计

### AddressSpace API

1. 创建Address Space
```http
POST /api/addressspaces
Content-Type: application/json

{
    "name": "string",
    "description": "string",
    "partitionId": "string"
}
```

2. 查询Address Space
```http
GET /api/addressspaces?name={name}&createdAfter={timestamp}
```

### Tag API

1. 创建或更新Tag
```http
PUT /api/addressspaces/{addressSpaceId}/tags/{tagName}
Content-Type: application/json

{
    "description": "string",
    "type": "Inheritable|NonInheritable",
    "knownValues": ["value1", "value2"],
    "implies": {
        "Region": {
            "AMS05": "EuropeWest"
        }
    }
}
```

### IP API

1. 创建IP节点
```http
POST /api/addressspaces/{addressSpaceId}/ips
Content-Type: application/json

{
    "prefix": "10.0.0.0/8",
    "tags": {
        "Environment": "Production",
        "Region": "USEast"
    }
}
```

## 客户端与CLI设计

### C#客户端
- 使用HttpClient调用API网关接口完成CRUD操作
- 提供简洁的命令行参数解析，支持批量操作

### PowerShell CLI
- 基于C#客户端封装
- 提供交互式命令行界面，方便管理员快速操作

## 单元及集成测试策略

### 单元测试
- 针对核心业务逻辑，例如数据校验、继承规则等编写测试
- 使用Mock对象隔离外部依赖

### 集成测试
- 模拟API调用场景，确保各服务接口的正确交互
- 覆盖常见使用流程及异常路径

## 日志与监控设计

- 集中日志记录：使用Azure Telemetry系统收集各服务日志
- 实时监控：基于Telemetry数据实现健康监控和告警
- 分布式跟踪：支持跨微服务调用的Trace信息收集

## 可扩展性与高可用性设计

- 采用微服务架构，各服务独立扩展
- 数据分区策略确保高效存储与检索
- 使用Docker、K8s及Azure Container Apps进行容器化部署，实现弹性伸缩和故障恢复

## 未来工作与改进方向

- 性能优化：针对大规模数据处理进行索引优化与查询优化
- 安全增强：扩展多因子认证和细粒度权限控制
- 灾备方案：设计数据库备份与灾难恢复机制
- 运维自动化：集成更多监控指标，实现自动化运维与预警
- API完善：持续优化接口文档与异常处理机制，增强扩展性

## 风险评估与应对措施

- 系统安全风险：通过多层次的安全认证、加密通信及严格的权限控制降低风险。
- 数据一致性风险：采用分布式事务和数据备份策略，确保数据高一致性和容灾能力。
- 性能瓶颈：定期进行性能测试及监控，利用自动化扩展策略应对流量高峰。
- 部署风险：通过CI/CD流水线和容器化部署减少人工失误，确保平滑发布。

## 开发与部署流程

- 开发流程：
  - 需求分析、系统设计及模块划分
  - 单元测试与集成测试驱动开发
  - 定期代码审核和静态代码分析
- 部署流程：
  - 使用Docker构建镜像
  - 通过CI/CD流水线自动部署到测试或生产环境
  - 利用Kubernetes进行滚动更新及弹性伸缩

## 文档与代码管理

- 使用Git进行版本控制，采用Git Flow管理分支和版本发布
- 系统文档持续更新，包含需求规格、设计文档及用户指南
- 代码审查机制确保代码质量，并定期进行安全与性能审查

## 业务逻辑与接口实现

- 业务逻辑层需要严格按照领域规则实现：
  - IP管理：实现IP地址的CIDR格式校验、树形结构构造、子节点继承父节点Inheritable tags，同时处理冲突校验与标签下沉逻辑。
  - Tag管理：根据Tag类型区分Inheritable与NonInheritable，同时对KnownValues进行值域验证。
  - Address Space管理：确保每个Address Space的数据在单一分区中存储，并对业务操作进行权限校验。
- 接口实现采用分层架构：
  - 控制层（Controller）：负责接收RESTful API请求，并调用领域服务。
  - 服务层（Service）：封装业务逻辑，包括数据校验、继承处理、跨服务调用等。
  - 数据访问层（Repository）：封装Azure Table Storage操作，实现数据分区和事务管理。
- 采用依赖注入、接口隔离等设计模式以提高模块解耦和测试能力。

## 测试策略与覆盖率

- 单元测试：基于Mock数据及依赖注入，覆盖业务逻辑的所有分支，目标代码覆盖率不少于90%。
  - 业务逻辑测试：验证IP校验、标签继承、冲突检测、下沉逻辑等。
  - 数据访问测试：模拟Azure Table Storage的交互，进行CRUD操作测试。
- 集成测试：测试各服务接口之间的交互，确保API网关、前端服务、数据访问服务之间的正确联动。
  - API接口测试：通过模拟真实请求检验RESTful API的正确性、错误处理、权限检查等。
- 功能测试与性能测试：使用自动化测试工具（如Selenium、Postman、JMeter）验证系统在并发、高流量情况下的稳定性与响应速度。

## 部署配置与自动化

- 部署流程：
  - 利用Docker构建各服务的镜像，每个服务维护独立的Dockerfile。
  - 通过CI/CD流水线（例如Azure DevOps或GitHub Actions）实现自动化构建、测试和部署。
  - 使用Kubernetes管理服务编排，配置滚动更新、弹性伸缩及故障恢复策略。
  - 部署到Azure Container Apps时，结合Azure Monitor和Telemetry进行实时监控与告警。

- 配置管理：
  - 系统配置集中化管理，通过环境变量或配置中心（如Azure App Configuration）实现不同环境的灵活切换。
  - 采用Secret管理工具（如Azure Key Vault）保护敏感信息，如数据库连接字符串、API密钥等。

- 自动化运维：
  - 利用日志聚合工具（例如Azure Log Analytics）对各服务日志进行集中收集和实时分析，辅助问题诊断。
  - 定期进行备份和灾难恢复演练，确保数据一致性和系统稳定性。

## 总结

本IPAM系统设计文档描述了一个基于微服务架构的企业级IP地址管理平台，涵盖了详细的模块划分、数据模型、API设计、客户端及CLI实现、测试策略和监控方案。此平台不仅满足当前企业应用需求，更具备高度的可扩展性和高可用性，为未来不断增长的数据和用户需求提供坚实保障。

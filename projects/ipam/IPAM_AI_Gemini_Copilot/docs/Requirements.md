# 概述
用C#和.Net Core实现一套企业级的IP地址管理系统IPAM，需要生成markdown格式的详细系统设计文档，创建代码项目并生成完整的代码库。

# 功能特性：
1. IP以CIDR形式存在一个address space里，每个address space是一颗以IPv6::/0为根的树，树的节点是IP CIDR，比如IPv4的根节点0.0.0.0/0是::/0的一个子节点。每个IP可带有任意多个Key-Value的tag信息。IP的关键属性包括AddressSpaceId, Id, Prefix, Tags, CreatedOn, ModifiedOn, ParentId，ChildrenIds
2. 每个address space有独立的tags集合, address space的关键属性包括Id, Name, Description, CreatedOn, ModifiedOn
3. 每个tag的属性包括AddressSpaceId，Name，Description，CreatedOn，ModifiedOn，Type, 可选值KnownValues， Attributes， Implies等
	1. Tag的类型Type分为Inheritable(可继承tag)和NonInheritable(不可继承tag)
	2. 对于可继承Tag, 子节点会继承父节点的Tag，父子节点的可继承tags不能有冲突，比如父节点带有Region=USEast的tag，那么子节点不能带有Region=USWest的tag。 为了节省存储空间，父节点的可继承Tags不会存储在子节点，当父节点被删除时，父节点的可继承tags会下沉到所有子节点上
	3. Tag的KnownValues属性表示tag value的可选集合，如果KnownValues为空，那么可以任意指定tag value, 如果不为空，那么tag value只能从可选集合里面选择。
	4. Inheritable Tag带有Implies属性，表示推断Imply关系，比如Datacenter=AMS05会推断出Region=EuropeWest,当我们在一个IP上添加Datacenter=AMS05时，Region=EuropeWest会自动添加到这个IP上。如果Tag1=V1推断Tag2=V2，并且Tag2=V2推断Tag3=V3，那么Tag1=V1需要推断出Tag3=V3, Implies是一个字典, key为推断的Tag名，value也是一个字典，value字典的key是Tag值，value字典的value是推断的tag的值，比如Datacenter=AMS05会推断出Region=EuropeWest， Datacenter Tag的Implies["Region"]["AMS05"]="EuropeWest"
	5. Inheritable Tag带有属性Attributes, Attributes是一个字典, key为属性名，value也是一个字典，value字典的key为Tag值, value字典的value是对应的属性值。比如Datacenter Tag带有名字为DisplayName的属性，Attributes["DisplayName"]["AMS05"]=Amsterdam05
5. 父子IP节点的CIDR可以一样，但是子节点带有的Inheritable tags需要比父节点至少多一个。
6. 支持address space的增删改查，address space的数量会不断增加，查询条件可包括：
	1. 指定AddressSpace Id。
	2. 指定Name关键字搜索
	3. CreatedOn大于或者小于指定的时间
7. 支持tag的增删改查，tag的数量会不断增加，查询条件可包括：
	1. 指定AddressSpace Id和完整的Tag Name
	2. 指定AddressSpace Id和Name的关键字搜索
8. 支持IP的增删改查，IP的创建和修改包括tags的增删，IP查询需要支持以下功能：
	1. 根据AddressSpace Id以及IP Id
	2. 根据AddressSpace Id以及IP CIDR
	3. 根据AddressSpace Id以及tags查询
	4. 根据AddressSpace Id以及IP Id查询子节点信息
9. IPAM系统需要支持用户认证，授权，限流，黑白名单功能。授权功能支持RBAC,不同的角色具有不同的权限，包括一下基本权限：
	1. SystemAdmin超级管理员，可以增删改查所有address space, IP和tag的
	2. AddressSpaceAdmin，指定address space的管理员，可以增删改查指定address space的属性，以及里面的tags和IP
	3. AddressSpaceViewer,指定address space的只读
10. 使用Azure table storage存储address space, tag和IP数据，实现数据的持久化，数据需要支持分区Partition来支持服务的可扩展，每个address space只能在一个分区partition里，一个partition可以带有多有address space
10. 提供Restful API用于address space, IP和tag的增删改查
11. 提供web portal用于address space, IP和tag的增删改查，使用asp.net Core和bootstrap + jQuery框架实现
12. 提供C# .Net Core客户端调用Restful API用于address space, IP和tag的增删改查
13. 提供Powersehll CLI客户端用于address space, IP和tag的增删改查
14. 系统采用微服务化架构，包括以下关键服务：
	1. API网关服务，用于请求路由，用户认证，鉴权，限流，黑白名单等
	2. Frontend服务，用于提供address space, tag和IP的CRUD的API
	3. DataAccess服务，用于和Azure table storage交互实现数据存取管理服务

# 非功能特性
15. 需要支持容器化部署，包括k8s和Azure container apps。
16. 使用接口隔离实现依赖注入，解决跨层依赖，隔离问题以及提高代码的高扩展性和可测试性。
17. 生成全面的单元测试，集成测试和功能测试用例，确保体代码覆盖率不小于90%
18. 生成全面的压力测试用例，给出系统负载极限。
19. 具备高可用，高性能和可扩展性，支持水平扩展，能够在不影响系统性能的情况下进行水平扩展。
20. 引入本地缓存用于提高系统性能。
21. 使用OpenTelemetry组件实现系统日志记录，支持微服务之间调用的链路追踪，并且与Azure telemetry系统集成，提供监控指标收集，分析以及基于telemetry的系统健康监控告警功能。
22. 提供系统的安装部署文档，用户手册，API文档，设计文档等。
23. 系统的性能指标包括响应时间，吞吐量，并发连接数，内存占用，CPU占用等。
24. 系统的安全指标包括数据加密，访问控制，身份认证，审计日志等。
25. 系统的高可用指标包括99.9%的 uptime, 99.9%的服务响应时间，99.9%的数据恢复时间等。
26. 系统的可扩展性指标包括支持100000个address space, 每个address space支持100000个tag, 每个address space支持10000000个IP

以下是该IPAM系统的设计文档大纲：
1. 系统架构设计
2. 数据库设计
3. 服务设计
4. 接口设计
5. 部署设计
6. 监控设计
7. 安全设计
8. 性能设计
9. 可扩展性设计
10. 高可用设计
11. 成本设计
12. 测试设计
13. 成本设计
14. 用户手册设计
15. API文档设计
16. 设计文档设计
17. 系统的成本设计
18. 系统的测试设计
19. 系统的用户手册设计
20. 系统的API文档设计
21. 系统的设计文档设计
# IP地址管理系统(IPAM)设计文档
## 1. 系统架构设计
## 2. 数据库设计
## 3. 服务设计
## 4. 接口设计
## 5. 部署设计
## 6. 监控设计
## 7. 安全设计
## 8. 性能设计
## 9. 可扩展性设计
## 10. 高可用设计
## 11. 成本设计
## 12. 测试设计
## 13. 成本设计
## 14. 用户手册设计
## 15. API文档设计
## 16. 设计文档设计
## 17. 系统的成本设计
## 18. 系统的测试设计
## 19. 系统的用户手册设计
## 20. 系统的API文档设计
## 21. 系统的设计文档设计
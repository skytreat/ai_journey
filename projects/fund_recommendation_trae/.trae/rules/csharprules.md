# 【规则名称】Trae C# 编码规范（全局生效）
## 规则基础信息
- 规则ID：trae_csharp_2026
- 生效范围：全局
- 违规级别：核心约束为 error，注释约束为 warning
- 适用场景：.NET 8/C# 12 开发

## 核心约束项
### 1. 基础格式（error）
- 缩进：4空格，禁用制表符
- 花括号：Allman 风格（单独成行）
- 行宽：代码≤120字符，注释≤80字符
- 命名空间：Company.{Project}.{Module}（如 Trae.FundAnalysis.Core）
- 空格：运算符两侧/参数逗号后加空格

### 2. 命名规则（error）
- 类/接口/枚举：大驼峰（如 FundAnalyzer、IFundService）
- 方法/属性：大驼峰（如 CalculateDrawdown()、NetValue）
- 变量/参数：小驼峰（如 fundNetValue、riskRate）
- 常量：大写蛇形（如 public const double RISK_FREE_RATE = 0.03）
- 私有字段：小驼峰+下划线前缀（如 _navData）
- 接口：以 I 开头（如 IDataValidator）
- 异常：以 Exception 结尾（如 DataParseException）

### 3. 语法规范（error）
- 现代语法：优先使用 record/init/文件级命名空间
- 空值处理：启用 nullable，用 ??/?. 处理空值
- 异步方法：加 Async 后缀（如 GetDataAsync()）
- 依赖注入：构造函数注入，禁用硬编码实例化
- 禁用语法：goto、魔法数字、未装箱值类型

### 4. 注释规范（warning）
- 公共成员：加 XML 文档注释（<summary>/<param>/<returns>）
- 私有逻辑：加单行注释，禁止重复注释

### 5. 安全约束（critical）
- 禁止硬编码密钥/连接字符串
- 参数校验：所有接口参数必须校验 null/空值
- SQL 操作：使用参数化查询，防止注入
- 非托管资源：实现 IDisposable 接口
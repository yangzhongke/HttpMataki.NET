# HttpMataki.NET

[English](README.md) | 中文

HttpMataki.NET— .NET程序的HTTP 通信的无声观察者。它能够完整记录请求和响应的头部和主体，而不会中断程序的执行。支持Https（无需配置或者信任任何证书）。

## 安装

### HttpMataki.NET.Auto (推荐)

```bash
dotnet add package HttpMataki.NET.Auto
```

### HttpMataki.NET

```bash
dotnet add package HttpMataki.NET
```

## 何时使用哪个包

### HttpMataki.NET.Auto (推荐)

- **使用场景**: 您希望在不修改现有代码的情况下自动记录 HTTP 日志
- **优势**:
  - 无需更改代码
  - 自动拦截所有 HTTP 请求/响应
  - 在后台静默工作
  - 非常适合调试和监控现有应用程序

### HttpMataki.NET

- **使用场景**: HttpMataki.NET.Auto 在您的环境中不工作或您需要更多控制
- **要求**: 您需要手动配置 HttpClient 与 HttpLoggingHandler
- **优势**:
  - 对日志记录行为有更明确的控制
  - 更好的兼容性

## 使用示例

### HttpMataki.NET.Auto 示例

HttpMataki.NET.Auto 无需任何代码更改即可自动拦截所有 HTTP 流量：

```csharp
using HttpMataki.NET.Auto;

HttpClientAutoInterceptor.StartInterception();

// 您现有的 HttpClient 代码保持不变
using var client1 = new HttpClient();
await client1.GetAsync("https://jsonplaceholder.typicode.com/posts/1");

// 所有 HTTP 请求和响应都会自动记录到控制台
// 无需更改代码！
```

### HttpMataki.NET 示例

HttpMataki.NET 需要手动配置 HttpClient：

```csharp
using HttpMataki.NET;

// 创建自定义日志记录操作
Action<string> customLogger = message =>
{
    Console.WriteLine($"[HTTP] {message}");
    // 您也可以记录到文件、数据库等
};

// 使用 HttpLoggingHandler 配置 HttpClient
var loggingHandler = new HttpLoggingHandler(customLogger);
using var client = new HttpClient(loggingHandler);

// 发起 HTTP 请求 - 它们将使用您的自定义记录器记录
var response = await client.PostAsync("https://api.example.com/users",
    new StringContent("{\"name\":\"John Doe\"}", Encoding.UTF8, "application/json"));
```

## 功能特性

- **全面的日志记录**: 捕获 URL、HTTP 方法、头部、状态码和请求/响应主体
- **多种内容类型支持**:
  - JSON 和文本内容（记录为文本）
  - 文件上传（multipart/form-data）- 将文件保存到临时目录
  - URL 编码表单 - 解析并显示表单字段
  - 图片 - 保存到临时目录并提供完整文件路径
  - 二进制内容 - 尽可能显示为原始文本
- **智能内容处理**: 自动检测并适当处理不同的内容类型
- **临时文件管理**: 自动将上传的文件和图片保存到有组织的临时目录
- **灵活的日志记录**: 自定义日志的写入位置和方式（控制台、文件、数据库等）

## 故障排除

1. **HttpMataki.NET.Auto 不工作？**
   - 尝试使用 HttpMataki.NET 替代
   - 确保拦截器在应用程序生命周期的早期初始化

2. **缺少日志？**
   - 验证日志记录操作是否正确配置
   - 检查 HTTP 请求是否确实通过 HttpClient 发起

## 许可证

本项目采用 MIT 许可证。

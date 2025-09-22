# HttpMataki.NET

HttpMataki.NET— a silent observer of HTTP communications. It can fully record the headers and bodies of requests and responses without disrupting the program's execution.

## Installation

### HttpMataki.NET.Auto (Recommended)

```bash
dotnet add package HttpMataki.NET.Auto
```

### HttpMataki.NET

```bash
dotnet add package HttpMataki.NET
```

## When to Use Which Package

### HttpMataki.NET.Auto (Recommended)

- **Use when**: You want automatic HTTP logging without modifying existing code
- **Benefits**:
  - Zero code changes required
  - Automatically intercepts all HTTP requests/responses
  - Works silently in the background
  - Perfect for debugging and monitoring existing applications

### HttpMataki.NET

- **Use when**: HttpMataki.NET.Auto doesn't work in your environment or you need more control
- **Requirements**: You need to manually configure HttpClient with HttpLoggingHandler
- **Benefits**:
  - More explicit control over logging behavior
  - More compatible

## Usage Examples

### HttpMataki.NET.Auto Example

HttpMataki.NET.Auto automatically intercepts all HTTP traffic without any code changes:

```csharp
using HttpMataki.NET.Auto;

HttpClientAutoInterceptor.StartInterception();

// Your existing HttpClient code works unchanged
using var client1 = new HttpClient();
await client1.GetAsync("https://jsonplaceholder.typicode.com/posts/1");

// All HTTP requests and responses are automatically logged to console
// No code changes required!
```

### HttpMataki.NET Example

HttpMataki.NET requires manual configuration of HttpClient:

```csharp
using HttpMataki.NET;

// Create a custom logging action
Action<string> customLogger = message =>
{
    Console.WriteLine($"[HTTP] {message}");
    // You can also log to files, databases, etc.
};

// Configure HttpClient with HttpLoggingHandler
var loggingHandler = new HttpLoggingHandler(customLogger);
using var client = new HttpClient(loggingHandler);

// Make HTTP requests - they will be logged using your custom logger
var response = await client.PostAsync("https://api.example.com/users",
    new StringContent("{\"name\":\"John Doe\"}", Encoding.UTF8, "application/json"));
```

## Features

- **Comprehensive Logging**: Captures URLs, HTTP methods, headers, status codes, and request/response bodies
- **Multiple Content Type Support**:
  - JSON and text content (logged as text)
  - File uploads (multipart/form-data) - saves files to temp directory
  - URL-encoded forms - parses and displays form fields
  - Images - saves to temp directory with full file paths
  - Binary content - displays as raw text when possible
- **Smart Content Handling**: Automatically detects and handles different content types appropriately
- **Temporary File Management**: Automatically saves uploaded files and images to organized temp directories
- **Flexible Logging**: Customize where and how logs are written (console, files, databases, etc.)

## Troubleshooting

1. **HttpMataki.NET.Auto not working?**
   - Try using HttpMataki.NET instead
   - Check if your application uses custom HttpClient configurations that might interfere
   - Ensure the interceptor is initialized early in your application lifecycle

2. **Missing logs?**
   - Verify the logging action is properly configured
   - Check if the HTTP requests are actually being made through HttpClient
   - Ensure the handler is properly added to the HttpClient pipeline

## License

This project is licensed under the MIT License.

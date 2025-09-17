# HttpMataki.NET
HttpMataki.NET— a silent observer of HTTP communications. It can fully record the headers and bodies of requests and responses without disrupting the program's execution.

## Features

- **Silent Observation**: Captures HTTP requests and responses without affecting application performance
- **Complete Recording**: Records headers, content, status codes, and timing information
- **Easy Integration**: Simple setup with HttpClient and dependency injection support
- **Flexible Storage**: Built-in in-memory observer with support for custom observers
- **Exception Handling**: Captures both successful responses and exceptions
- **Thread-Safe**: Safe for use in multi-threaded applications

## Installation

Install the HttpMataki.NET package from NuGet:

```bash
dotnet add package HttpMataki.NET
```

## Quick Start

### Basic Usage

```csharp
using HttpMataki.NET;

// Create an observer
var observer = new InMemoryHttpObserver();

// Create an HttpClient with observer
using var client = HttpMatakiExtensions.CreateObservedHttpClient(observer);

// Make HTTP requests - they will be automatically observed
var response = await client.GetAsync("https://api.example.com/data");

// Access recorded communications
var records = observer.GetRecords();
foreach (var record in records)
{
    Console.WriteLine($"{record.Request.Method} {record.Request.Uri}");
    Console.WriteLine($"Status: {record.Response?.StatusCode}");
    Console.WriteLine($"Duration: {record.Duration?.TotalMilliseconds}ms");
}
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using HttpMataki.NET;

// Configure services
services.AddHttpMataki();
services.AddHttpClient<MyApiService>()
    .AddHttpMessageHandler<HttpObserverHandler>();

// Use in your service
public class MyApiService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpObserver _observer;

    public MyApiService(HttpClient httpClient, IHttpObserver observer)
    {
        _httpClient = httpClient;
        _observer = observer;
    }

    public async Task<string> GetDataAsync()
    {
        var response = await _httpClient.GetAsync("https://api.example.com/data");
        // HTTP communication is automatically recorded
        return await response.Content.ReadAsStringAsync();
    }
}
```

### Custom Observer

```csharp
public class FileHttpObserver : IHttpObserver
{
    public async Task OnHttpCommunicationAsync(HttpCommunicationRecord record)
    {
        var json = JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync($"http-{record.Id}.json", json);
    }
}

// Use custom observer
services.AddHttpMataki(new FileHttpObserver());
```

## API Reference

### Core Classes

- **`IHttpObserver`**: Interface for implementing custom observers
- **`InMemoryHttpObserver`**: Built-in observer that stores records in memory
- **`HttpObserverHandler`**: HTTP message handler that performs the observation
- **`HttpCommunicationRecord`**: Contains complete HTTP communication data
- **`HttpRequestData`**: HTTP request information
- **`HttpResponseData`**: HTTP response information

### Extension Methods

- **`CreateObservedHttpClient(observer)`**: Creates HttpClient with observer
- **`AddHttpMataki()`**: Registers services with DI container

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
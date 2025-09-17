using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace HttpMataki.NET.IntegrationTests;

public class HttpObserverIntegrationTests
{
    [Fact]
    public async Task HttpObserver_WithMockedHttpCall_CapturesAllData()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(HttpStatusCode.OK, "{ \"url\": \"https://test.example.com/get\", \"headers\": {} }");
        
        using var client = HttpMatakiExtensions.CreateObservedHttpClient(observer, mockHandler);

        // Act
        var response = await client.GetAsync("https://test.example.com/get");

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Equal("GET", record.Request.Method);
        Assert.Equal("https://test.example.com/get", record.Request.Uri);
        Assert.NotNull(record.Response);
        Assert.Equal(200, record.Response.StatusCode);
        Assert.NotNull(record.Duration);
        Assert.True(record.Duration.Value.TotalMilliseconds > 0);
        
        // Verify response content was captured
        Assert.NotNull(record.Response.Content);
        Assert.Contains("test.example.com", record.Response.Content);
    }

    [Fact]
    public async Task HttpObserver_WithPostRequest_CapturesRequestAndResponseContent()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(HttpStatusCode.OK, "{ \"json\": { \"name\": \"test\", \"value\": 123 } }");
        
        using var client = HttpMatakiExtensions.CreateObservedHttpClient(observer, mockHandler);
        
        var postData = new { name = "test", value = 123 };
        var json = JsonSerializer.Serialize(postData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("https://test.example.com/post", content);

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Equal("POST", record.Request.Method);
        Assert.Equal("https://test.example.com/post", record.Request.Uri);
        Assert.Contains(json, record.Request.Content);
        
        Assert.NotNull(record.Response);
        Assert.Equal(200, record.Response.StatusCode);
        Assert.Contains("test", record.Response.Content);
    }

    [Fact]
    public async Task HttpObserver_WithMultipleRequests_CapturesAll()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var mockHandler = new MockHttpMessageHandler();
        
        // Setup different responses for different calls
        mockHandler.SetupSequentialResponses(
            (HttpStatusCode.OK, "{ \"endpoint\": \"get\" }"),
            (HttpStatusCode.Created, "{ \"endpoint\": \"status/201\" }"),
            (HttpStatusCode.OK, "{ \"endpoint\": \"post\", \"data\": \"test data\" }")
        );
        
        using var client = HttpMatakiExtensions.CreateObservedHttpClient(observer, mockHandler);

        // Act - Make multiple requests
        var response1 = await client.GetAsync("https://test.example.com/get");
        var response2 = await client.GetAsync("https://test.example.com/status/201");
        var response3 = await client.PostAsync("https://test.example.com/post", 
            new StringContent("test data", Encoding.UTF8, "text/plain"));

        // Assert
        var records = observer.GetRecords();
        Assert.Equal(3, records.Count);
        
        // Check first request
        var record1 = records[0];
        Assert.Equal("GET", record1.Request.Method);
        Assert.Contains("/get", record1.Request.Uri);
        Assert.Equal(200, record1.Response!.StatusCode);
        
        // Check second request  
        var record2 = records[1];
        Assert.Equal("GET", record2.Request.Method);
        Assert.Contains("/status/201", record2.Request.Uri);
        Assert.Equal(201, record2.Response!.StatusCode);
        
        // Check third request
        var record3 = records[2];
        Assert.Equal("POST", record3.Request.Method);
        Assert.Contains("/post", record3.Request.Uri);
        Assert.Equal("test data", record3.Request.Content);
        Assert.Equal(200, record3.Response!.StatusCode);
    }

    [Fact]
    public async Task HttpObserver_WithHttpError_CapturesErrorResponse()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(HttpStatusCode.NotFound, "Not Found");
        
        using var client = HttpMatakiExtensions.CreateObservedHttpClient(observer, mockHandler);

        // Act - Make request that returns 404
        var response = await client.GetAsync("https://test.example.com/status/404");

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Equal("GET", record.Request.Method);
        Assert.Contains("/status/404", record.Request.Uri);
        Assert.NotNull(record.Response);
        Assert.Equal(404, record.Response.StatusCode);
        Assert.Null(record.Exception); // 404 is not an exception, just a status code
    }

    [Fact]
    public async Task HttpObserver_WithCustomHeaders_CapturesHeaders()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(HttpStatusCode.OK, "{ \"headers\": { \"X-Custom-Header\": \"custom-value\" } }");
        
        using var client = HttpMatakiExtensions.CreateObservedHttpClient(observer, mockHandler);
        
        // Add custom headers
        client.DefaultRequestHeaders.Add("X-Custom-Header", "custom-value");
        client.DefaultRequestHeaders.Add("User-Agent", "HttpMataki.NET-Test/1.0");

        // Act
        var response = await client.GetAsync("https://test.example.com/headers");

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Contains("X-Custom-Header", record.Request.Headers.Keys);
        Assert.Equal("custom-value", record.Request.Headers["X-Custom-Header"][0]);
        Assert.Contains("User-Agent", record.Request.Headers.Keys);
        Assert.Contains("HttpMataki.NET-Test/1.0", record.Request.Headers["User-Agent"][0]);
        
        // Verify the response shows our headers were captured
        Assert.NotNull(record.Response?.Content);
        Assert.Contains("X-Custom-Header", record.Response.Content);
    }

    [Fact]
    public async Task HttpObserver_WithDependencyInjection_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddHttpMataki();
        
        // Add mock handler for testing
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupResponse(HttpStatusCode.OK, "{ \"test\": \"data\" }");
        
        services.AddHttpClient<TestHttpService>()
            .ConfigurePrimaryHttpMessageHandler(() => mockHandler)
            .AddHttpMessageHandler<HttpObserverHandler>();
        
        var serviceProvider = services.BuildServiceProvider();
        var httpService = serviceProvider.GetRequiredService<TestHttpService>();
        var observer = serviceProvider.GetRequiredService<IHttpObserver>() as InMemoryHttpObserver;

        // Act
        await httpService.GetDataAsync();

        // Assert
        Assert.NotNull(observer);
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Equal("GET", record.Request.Method);
        Assert.Equal("https://test.example.com/get", record.Request.Uri);
        Assert.NotNull(record.Response);
        Assert.Equal(200, record.Response.StatusCode);
    }

    [Fact]
    public async Task HttpObserver_WithNetworkException_CapturesException()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SetupException(new HttpRequestException("Network error"));
        
        using var client = HttpMatakiExtensions.CreateObservedHttpClient(observer, mockHandler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => 
            await client.GetAsync("https://test.example.com/error"));

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Equal("GET", record.Request.Method);
        Assert.NotNull(record.Exception);
        Assert.IsType<HttpRequestException>(record.Exception);
        Assert.Contains("Network error", record.Exception.Message);
        Assert.Null(record.Response);
    }

    private class TestHttpService
    {
        private readonly HttpClient _httpClient;

        public TestHttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetDataAsync()
        {
            var response = await _httpClient.GetAsync("https://test.example.com/get");
            return await response.Content.ReadAsStringAsync();
        }
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private Queue<(HttpStatusCode statusCode, string content)> _responses = new();
        private Exception? _exception;

        public void SetupResponse(HttpStatusCode statusCode, string content)
        {
            _responses.Enqueue((statusCode, content));
        }

        public void SetupSequentialResponses(params (HttpStatusCode statusCode, string content)[] responses)
        {
            _responses.Clear();
            foreach (var response in responses)
            {
                _responses.Enqueue(response);
            }
        }

        public void SetupException(Exception exception)
        {
            _exception = exception;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken); // Simulate network delay

            if (_exception != null)
            {
                throw _exception;
            }

            if (_responses.Count == 0)
            {
                throw new InvalidOperationException("No mock response configured");
            }

            var (statusCode, content) = _responses.Dequeue();
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            return response;
        }
    }
}
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace HttpMataki.NET.UnitTests;

public class HttpObserverHandlerTests
{
    [Fact]
    public void Constructor_WithNullObserver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new HttpObserverHandler(null!));
    }

    [Fact]
    public async Task SendAsync_WithValidRequest_CapturesRequestData()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var handler = new HttpObserverHandler(observer)
        {
            InnerHandler = new TestMessageHandler(new HttpResponseMessage(HttpStatusCode.OK))
        };
        
        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");
        request.Headers.Add("X-Test-Header", "test-value");

        // Act
        await client.SendAsync(request);

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Equal("GET", record.Request.Method);
        Assert.Equal("https://example.com/test", record.Request.Uri);
        Assert.Contains("X-Test-Header", record.Request.Headers.Keys);
        Assert.Equal("test-value", record.Request.Headers["X-Test-Header"][0]);
    }

    [Fact]
    public async Task SendAsync_WithRequestContent_CapturesContent()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var handler = new HttpObserverHandler(observer)
        {
            InnerHandler = new TestMessageHandler(new HttpResponseMessage(HttpStatusCode.OK))
        };
        
        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/test");
        request.Content = new StringContent("test content", Encoding.UTF8, "text/plain");

        // Act
        await client.SendAsync(request);

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.Equal("test content", record.Request.Content);
    }

    [Fact]
    public async Task SendAsync_WithResponse_CapturesResponseData()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var response = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("response content", Encoding.UTF8, "application/json")
        };
        response.Headers.Add("X-Response-Header", "response-value");
        
        var handler = new HttpObserverHandler(observer)
        {
            InnerHandler = new TestMessageHandler(response)
        };
        
        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/test");

        // Act
        await client.SendAsync(request);

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.NotNull(record.Response);
        Assert.Equal(201, record.Response.StatusCode);
        Assert.Equal("response content", record.Response.Content);
        Assert.Contains("X-Response-Header", record.Response.Headers.Keys);
    }

    [Fact]
    public async Task SendAsync_WithException_CapturesException()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var handler = new HttpObserverHandler(observer)
        {
            InnerHandler = new TestMessageHandler(new Exception("Test exception"))
        };
        
        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await client.SendAsync(request));

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.NotNull(record.Exception);
        Assert.Equal("Test exception", record.Exception.Message);
        Assert.Null(record.Response);
    }

    [Fact]
    public async Task SendAsync_CapturesDuration()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var handler = new HttpObserverHandler(observer)
        {
            InnerHandler = new TestMessageHandler(new HttpResponseMessage(HttpStatusCode.OK), delay: TimeSpan.FromMilliseconds(100))
        };
        
        var client = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

        // Act
        await client.SendAsync(request);

        // Assert
        var records = observer.GetRecords();
        Assert.Single(records);
        
        var record = records[0];
        Assert.NotNull(record.Duration);
        Assert.True(record.Duration.Value.TotalMilliseconds >= 50); // Allow some variance
    }

    private class TestMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Exception? _exception;
        private readonly TimeSpan _delay;

        public TestMessageHandler(HttpResponseMessage response, TimeSpan delay = default)
        {
            _response = response;
            _delay = delay;
        }

        public TestMessageHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            if (_exception != null)
            {
                throw _exception;
            }

            return _response!;
        }
    }
}
namespace HttpMataki.NET.UnitTests;

public class HttpLoggingHandlerTests
{
    [Fact]
    public void Constructor_Default_SetsConsoleLogAction()
    {
        var handler = new HttpLoggingHandler();
        Assert.NotNull(handler);
    }

    [Fact]
    public void Constructor_LogFilePath_SetsFileLogAction()
    {
        var tempFile = Path.GetTempFileName();
        var handler = new HttpLoggingHandler((string)tempFile);
        Assert.NotNull(handler);
        File.Delete(tempFile);
    }

    [Fact]
    public void Constructor_LogFilePath_Empty_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new HttpLoggingHandler((string)""));
    }

    [Fact]
    public void Constructor_LogActionNull_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => new HttpLoggingHandler((Action<string>)null));
    }

    [Fact]
    public async Task SendAsync_LogsRequestBodyToDelegate()
    {
        var logs = new List<string>();
        Action<string> logAction = msg => logs.Add(msg);
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("response body", System.Text.Encoding.UTF8, "application/json")
        };
        responseMessage.Headers.Add("Custom-Response-Header", "response-value");
        
        var handler = new HttpLoggingHandler((Action<string>)logAction)
        {
            InnerHandler = new DummyHandler(responseMessage)
        };
        using var client = new HttpClient(handler);
        var content = new StringContent("test body", System.Text.Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, "http://test") { Content = content };
        request.Headers.Add("Custom-Request-Header", "request-value");
        
        await client.SendAsync(request);
        
        // Assert Request logs
        Assert.Contains(logs, l => l.Contains("Request:"));
        Assert.Contains(logs, l => l.Contains("Method: POST"));
        Assert.Contains(logs, l => l.Contains("URL: http://test/"));
        Assert.Contains(logs, l => l.Contains("Custom-Request-Header: request-value"));
        Assert.Contains(logs, l => l.Contains("Body: test body"));
        
        // Assert Response logs
        Assert.Contains(logs, l => l.Contains("Response:"));
        Assert.Contains(logs, l => l.Contains("Status Code: 200 OK"));
        Assert.Contains(logs, l => l.Contains("Custom-Response-Header: response-value"));
        Assert.Contains(logs, l => l.Contains("Body: response body"));
    }

    [Fact]
    public async Task SendAsync_HandlesFileUpload()
    {
        var logs = new List<string>();
        Action<string> logAction = msg => logs.Add(msg);
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        
        var handler = new HttpLoggingHandler((Action<string>)logAction)
        {
            InnerHandler = new DummyHandler(responseMessage)
        };
        
        using var client = new HttpClient(handler);
        
        // Create multipart form data content with file
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("test file content"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        multipartContent.Add(fileContent, "file", "test.txt");
        multipartContent.Add(new StringContent("field value"), "textField");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "http://test") { Content = multipartContent };
        
        await client.SendAsync(request);
        
        // Assert multipart content logs
        Assert.Contains(logs, l => l.Contains("Multipart Form Data Content:"));
        Assert.Contains(logs, l => l.Contains("Field: file"));
        Assert.Contains(logs, l => l.Contains("Original FileName: test.txt"));
        Assert.Contains(logs, l => l.Contains("Content-Type: text/plain"));
        Assert.Contains(logs, l => l.Contains("Saved to:"));
        Assert.Contains(logs, l => l.Contains("File Size: 17 bytes"));
        Assert.Contains(logs, l => l.Contains("Field: textField"));
        Assert.Contains(logs, l => l.Contains("Value: field value"));
    }

    private class DummyHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public DummyHandler()
        {
            _response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        }

        public DummyHandler(HttpResponseMessage response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }
}
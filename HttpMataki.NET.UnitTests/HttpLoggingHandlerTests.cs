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
        var handler = new HttpLoggingHandler((Action<string>)logAction)
        {
            InnerHandler = new DummyHandler()
        };
        var client = new HttpClient(handler);
        var content = new StringContent("test body", System.Text.Encoding.UTF8, "application/json");
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://test") { Content = content });
        Assert.Contains(logs, l => l.Contains("Request:"));
        Assert.Contains(logs, l => l.Contains("Body: test body"));
    }

    private class DummyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
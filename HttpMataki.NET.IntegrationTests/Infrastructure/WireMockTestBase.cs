using WireMock.Admin.Requests;
using WireMock.Server;
using WireMock.Settings;
using WireMock.Logging;
using Xunit;

namespace HttpMataki.NET.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for WireMock integration tests providing common server setup and teardown
/// </summary>
public abstract class WireMockTestBase : IDisposable
{
    protected WireMockServer Server { get; private set; }
    protected string ServerUrl => Server.Url!;
    protected readonly List<string> LogMessages = new();

    protected WireMockTestBase()
    {
        // Start WireMock server on a random port
        Server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Use random available port
            StartAdminInterface = true,
            Logger = new WireMockNullLogger()
        });
    }

    /// <summary>
    /// Create HttpLoggingHandler that captures log messages for testing
    /// </summary>
    protected HttpMataki.NET.HttpLoggingHandler CreateTestHandler()
        => new HttpMataki.NET.HttpLoggingHandler(message => LogMessages.Add(message));

    /// <summary>
    /// Clear all log messages
    /// </summary>
    protected void ClearLogs() => LogMessages.Clear();

    /// <summary>
    /// Assert that log messages contain expected content
    /// </summary>
    protected void AssertLogContains(string expectedContent)
    {
        Assert.Contains(LogMessages, msg => msg.Contains(expectedContent));
    }

    /// <summary>
    /// Assert that log messages contain all expected contents
    /// </summary>
    protected void AssertLogContainsAll(params string[] expectedContents)
    {
        foreach (var content in expectedContents)
        {
            AssertLogContains(content);
        }
    }

    public virtual void Dispose()
    {
        Server?.Stop();
        Server?.Dispose();
    }
}

/// <summary>
/// Null logger for WireMock to suppress console output during tests
/// </summary>
internal class WireMockNullLogger : IWireMockLogger
{
    public void Debug(string formatString, params object[] args)
    {
    }

    public void Info(string formatString, params object[] args)
    {
    }

    public void Warn(string formatString, params object[] args)
    {
    }

    public void Error(string formatString, params object[] args)
    {
    }

    public void Error(string message, Exception exception)
    {
    }

    public void DebugRequestResponse(LogEntryModel logEntryModel, bool isAdminRequest)
    {
    }
}
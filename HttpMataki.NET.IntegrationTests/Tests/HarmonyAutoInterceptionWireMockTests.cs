using HttpMataki.NET.Auto;
using HttpMataki.NET.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HttpMataki.NET.IntegrationTests.Tests;

/// <summary>
///     Integration tests for Harmony auto-interception using WireMock server
///     Based on HarmonyAutoInterceptionDemoAsync() from the demo project
/// </summary>
[Collection("Harmony Tests")]
public class HarmonyAutoInterceptionWireMockTests : WireMockTestBase
{
    public HarmonyAutoInterceptionWireMockTests()
    {
        // Setup all required endpoints
        TestDataHelper.SetupJsonPlaceholderEndpoints(Server);
        TestDataHelper.SetupPostEcho(Server);

        // Start Harmony interception with custom logging for testing
        HttpClientAutoInterceptor.StartInterception(() => new HttpLoggingHandler(message => LogMessages.Add(message)));
    }

    [Fact]
    public async Task AutoInterception_Should_Log_Direct_HttpClient_Creation()
    {
        // Arrange & Act - Test 1: Direct HttpClient creation
        using var client1 = new HttpClient();
        await client1.GetAsync($"{ServerUrl}/posts/1");

        // Assert
        AssertLogContainsAll(
            "GET",
            "/posts/1",
            "Request:",
            "Response:"
        );
    }

    [Fact]
    public async Task AutoInterception_Should_Log_HttpClient_With_Custom_Handler()
    {
        // Arrange
        ClearLogs();
        var customHandler = new HttpClientHandler { UseCookies = false };

        // Act - Test 2: HttpClient with custom handler
        using var client2 = new HttpClient(customHandler);
        await client2.GetAsync($"{ServerUrl}/posts/2");

        // Assert
        AssertLogContainsAll(
            "GET",
            "/posts/2",
            "Request:",
            "Response:"
        );
    }

    [Fact]
    public async Task AutoInterception_Should_Log_HttpClientFactory_Requests()
    {
        // Arrange
        ClearLogs();
        var services = new ServiceCollection();
        services.AddHttpClient("test");
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act - Test 4: HttpClientFactory
        using var client4 = factory.CreateClient("test");
        await client4.GetAsync($"{ServerUrl}/posts/3");

        // Assert
        AssertLogContainsAll(
            "GET",
            "/posts/3",
            "Request:",
            "Response:"
        );
    }

    [Fact]
    public async Task AutoInterception_Should_Handle_Multiple_Concurrent_Requests()
    {
        // Arrange
        ClearLogs();
        var tasks = new List<Task>();

        // Act - Create multiple HttpClient instances concurrently
        for (var i = 0; i < 3; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var client = new HttpClient();
                await client.GetAsync($"{ServerUrl}/posts/{index + 1}");
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var getRequests = LogMessages.Where(msg => msg.Contains("GET")).Count();
        Assert.True(getRequests >= 3); // At least 3 GET requests should be logged
        AssertLogContains("Request:");
        AssertLogContains("Response:");
    }

    [Fact]
    public async Task AutoInterception_Should_Log_POST_Requests_With_Body()
    {
        // Arrange
        ClearLogs();
        using var client = new HttpClient();
        var postData = new
        {
            userId = 1,
            title = "Auto-Intercepted Post",
            body = "This request was automatically intercepted by HttpMataki.NET.Auto!"
        };

        // Act
        var response = await client.PostAsJsonAsync($"{ServerUrl}/posts", postData);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        AssertLogContainsAll(
            "POST",
            "/posts",
            "Auto-Intercepted Post",
            "application/json",
            "Request:",
            "Response:"
        );
    }

    [Fact]
    public void AutoInterception_Should_Report_Active_Status()
    {
        // Assert
        Assert.True(HttpClientAutoInterceptor.IsInterceptionActive);
    }

    public override void Dispose()
    {
        // Clean up - stop interception after tests
        if (HttpClientAutoInterceptor.IsInterceptionActive)
        {
            HttpClientAutoInterceptor.StopInterception();
        }

        base.Dispose();
    }
}
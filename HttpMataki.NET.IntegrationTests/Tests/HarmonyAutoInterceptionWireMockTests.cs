using HttpMataki.NET.Auto;
using HttpMataki.NET.IntegrationTests.Infrastructure;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HttpMataki.NET.IntegrationTests.Tests
{
    /// <summary>
    /// Integration tests for Harmony auto-interception using WireMock server
    /// Only available on .NET 5.0 and later frameworks
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
            HttpClientAutoInterceptor.StartInterception(() => new HttpMataki.NET.HttpLoggingHandler(message => LogMessages.Add(message)));
        }

        [Fact]
        public async Task AutoInterception_Should_Handle_Custom_Handler_On_Modern_Frameworks()
        {
            // Arrange
            ClearLogs();
            var customHandler = new HttpClientHandler() { UseCookies = false };
        
            // Act
            using (var client2 = new HttpClient(customHandler))
            {
                await client2.GetAsync($"{ServerUrl}/posts/2");

                // Assert
                AssertLogContainsAll(
                    "GET",
                    "/posts/2",
                    "Request:",
                    "Response:"
                );
            }
        }
#if NET5_0_OR_GREATER

        [Fact]
        public async Task AutoInterception_Should_Work_With_HttpClientFactory()
        {
            // Arrange
            ClearLogs();
            var services = new ServiceCollection();
            services.AddHttpClient("compatibility-test");
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IHttpClientFactory>();
        
            // Act
            using var client = factory.CreateClient("compatibility-test");
            await client.GetAsync($"{ServerUrl}/posts/3");

            // Assert
            AssertLogContainsAll(
                "GET",
                "/posts/3",
                "Request:",
                "Response:"
            );
        }
#endif

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
}

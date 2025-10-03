using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HttpMataki.NET.IntegrationTests.Infrastructure;
using Xunit;

namespace HttpMataki.NET.IntegrationTests.Tests
{
    /// <summary>
    ///     Integration tests for error handling and special scenarios using WireMock server
    /// </summary>
    public class SpecialScenariosWireMockTests : WireMockTestBase
    {
        public SpecialScenariosWireMockTests()
        {
            // Setup all required endpoints including error scenarios
            TestDataHelper.SetupPostEcho(Server);
            TestDataHelper.SetupErrorResponses(Server);
            TestDataHelper.SetupContentTypeResponses(Server);
            TestDataHelper.SetupJsonPlaceholderEndpoints(Server);
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_404_Error_Response()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{ServerUrl}/error/404");

                // Assert
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                AssertLogContainsAll(
                    "GET",
                    "/error/404",
                    "404",
                    "Not Found",
                    "Request:",
                    "Response:"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_500_Error_Response()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{ServerUrl}/error/500");

                // Assert
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                AssertLogContainsAll(
                    "GET",
                    "/error/500",
                    "500",
                    "Internal Server Error",
                    "Request:",
                    "Response:"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Empty_Content_Type()
        {
            // Arrange
            var handler = CreateTestHandler();

            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new StringContent("Content without explicit type", Encoding.UTF8)
                };
                // Remove content type to simulate null content type
                request.Content.Headers.ContentType = null;

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "Null or empty Content-Type header",
                    "Content without explicit type",
                    "POST",
                    "/post"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Binary_Content()
        {
            // Arrange
            var handler = CreateTestHandler();
            using (var client = new HttpClient(handler))
            {
                var binaryData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new ByteArrayContent(binaryData)
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "application/octet-stream",
                    "POST",
                    "/post",
                    "Request:",
                    "Response:"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Chinese_Content()
        {
            // Arrange
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var handler = CreateTestHandler();
            using (var client = new HttpClient(handler))
            {
                var chineseContent = "这是中文测试内容，用于验证编码处理";

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new StringContent(chineseContent, Encoding.UTF8, "text/plain")
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    chineseContent,
                    "text/plain",
                    "POST",
                    "/post"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Unicode_JSON()
        {
            // Arrange
            var handler = CreateTestHandler();
            using (var client = new HttpClient(handler))
            {
                var jsonWithUnicode = "{\"message\":\"Hello 世界\",\"emoji\":\"🌍\",\"special\":\"café\"}";

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new StringContent(jsonWithUnicode, Encoding.UTF8, "application/json")
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "Hello 世界",
                    "🌍",
                    "café",
                    "application/json"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Form_Data_With_Special_Characters()
        {
            // Arrange
            var handler = CreateTestHandler();

            using (var client = new HttpClient(handler))
            {
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("name", "Test User"),
                    new KeyValuePair<string, string>("email", "test+user@example.com"),
                    new KeyValuePair<string, string>("description", "Special chars: @#$%^&*()"),
                    new KeyValuePair<string, string>("unicode", "Unicode: 测试用户")
                };
                var formContent = new FormUrlEncodedContent(formData);

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = formContent
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "application/x-www-form-urlencoded",
                    "Test User",
                    "test+user@example.com",
                    "Special chars: @#$%^&*()",
                    "Unicode: 测试用户"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Very_Large_Request_Body()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Create a large string (1MB)
            using (var client = new HttpClient(handler))
            {
                var largeContent = new string('A', 1024 * 1024);
                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new StringContent(largeContent, Encoding.UTF8, "text/plain")
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "POST",
                    "/post",
                    "text/plain",
                    "Request:",
                    "Response:"
                );

                // Verify that large content is handled (at least partially logged)
                Assert.Contains(LogMessages, msg => msg.Contains("AAAA"));
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Multiple_Sequential_Requests()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act - Make multiple sequential requests
            using (var client = new HttpClient(handler))
            {
                await client.GetAsync($"{ServerUrl}/posts/1");
                await client.PostAsJsonAsync($"{ServerUrl}/posts", new { title = "Test" });
                await client.GetAsync($"{ServerUrl}/users");

                // Assert
                var requestCount = LogMessages.Count(msg => msg.Contains("Request:"));
                var responseCount = LogMessages.Count(msg => msg.Contains("Response:"));

                Assert.Equal(3, requestCount);
                Assert.Equal(3, responseCount);

                AssertLogContainsAll(
                    "GET",
                    "POST",
                    "/posts/1",
                    "/posts",
                    "/users"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Preserve_Response_Content()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{ServerUrl}/posts/1");
                var content = await response.Content.ReadAsStringAsync();

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                Assert.NotNull(content);
                Assert.NotEmpty(content);

                // Verify the response content can still be read after logging
                Assert.Contains("Test Post Title", content);
                AssertLogContainsAll(
                    "GET",
                    "/posts/1",
                    "Request:",
                    "Response:"
                );
            }
        }
    }
}
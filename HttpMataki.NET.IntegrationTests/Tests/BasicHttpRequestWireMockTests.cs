using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HttpMataki.NET.IntegrationTests.Infrastructure;
using Xunit;

namespace HttpMataki.NET.IntegrationTests.Tests
{
    /// <summary>
    ///     Integration tests for basic HTTP requests using WireMock server
    ///     Based on BasicDemo1Async() from the demo project
    /// </summary>
    public class BasicHttpRequestWireMockTests : WireMockTestBase
    {
        public BasicHttpRequestWireMockTests()
        {
            // Setup all required endpoints
            TestDataHelper.SetupPostEcho(Server);
            TestDataHelper.SetupImageResponse(Server);
            TestDataHelper.SetupContentTypeResponses(Server);
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_Text_Request()
        {
            // Arrange
            var handler = CreateTestHandler();
            using (var client = new HttpClient(handler))
            {
                var textContent = "Hello, this is plain text!";

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new StringContent(textContent, Encoding.UTF8, "text/plain")
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "POST",
                    "/post",
                    "text/plain",
                    textContent,
                    "Request:",
                    "Response:",
                    "200"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_JSON_Request()
        {
            // Arrange
            var handler = CreateTestHandler();
            using (var client = new HttpClient(handler))
            {
                var jsonContent = "{\"name\":\"Mataki\",\"type\":\"json\"}";

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "POST",
                    "/post",
                    "application/json",
                    "Mataki",
                    "json",
                    "Request:",
                    "Response:"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_Image_Request()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{ServerUrl}/image/jpeg");

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "GET",
                    "/image/jpeg",
                    "image/jpeg",
                    "Request:",
                    "Response:",
                    "Image Content Type"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_Multipart_Form_Data()
        {
            // Arrange
            var handler = CreateTestHandler();

            using (var client = new HttpClient(handler))
            {
                var multipartContent = new MultipartFormDataContent();
                var fileBytes = Encoding.UTF8.GetBytes("This is a demo file.");
                var fileContent = new ByteArrayContent(fileBytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                multipartContent.Add(fileContent, "file", "demo.txt");
                multipartContent.Add(new StringContent("field value"), "textField");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = multipartContent
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "POST",
                    "/post",
                    "multipart/form-data",
                    "demo.txt",
                    "field value",
                    "Multipart Form Data Content:",
                    "Field: file",
                    "Field: textField"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_Form_URL_Encoded()
        {
            // Arrange
            var handler = CreateTestHandler();

            using (var client = new HttpClient(handler))
            {
                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("name", "Mataki"),
                    new KeyValuePair<string, string>("email", "mataki@example.com"),
                    new KeyValuePair<string, string>("age", "28")
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
                    "POST",
                    "/post",
                    "application/x-www-form-urlencoded",
                    "Mataki",
                    "mataki@example.com",
                    "28",
                    "Form URL Encoded Content:",
                    "name: Mataki",
                    "email: mataki@example.com",
                    "age: 28"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_XML_Content()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{ServerUrl}/xml");

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "GET",
                    "/xml",
                    "application/xml",
                    "XML response from WireMock",
                    "Request:",
                    "Response:"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Plain_Text_Response()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{ServerUrl}/text");

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "GET",
                    "/text",
                    "text/plain",
                    "Plain text response from WireMock",
                    "Request:",
                    "Response:"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_GraphQL_Request()
        {
            // Arrange
            var handler = CreateTestHandler();
            using (var client = new HttpClient(handler))
            {
                var graphqlQuery = "query { user(id: 1) { name email } }";

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/graphql")
                {
                    Content = new StringContent(graphqlQuery, Encoding.UTF8, "application/graphql")
                };

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "POST",
                    "/graphql",
                    "application/graphql",
                    "query { user(id: 1)",
                    "Request:",
                    "Response:"
                );
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Handle_Large_JSON_Response()
        {
            // Arrange
            var handler = CreateTestHandler();

            // Act
            using (var client = new HttpClient(handler))
            {
                var response = await client.GetAsync($"{ServerUrl}/large-json");

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "GET",
                    "/large-json",
                    "application/json",
                    "Request:",
                    "Response:"
                );

                // Should handle large responses without issues
                var responseContent = await response.Content.ReadAsStringAsync();
                Assert.Contains("items", responseContent);
                Assert.Contains("total", responseContent);
            }
        }

        [Fact]
        public async Task HttpLoggingHandler_Should_Log_Custom_Headers()
        {
            // Arrange
            var handler = CreateTestHandler();

            using (var client = new HttpClient(handler))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{ServerUrl}/post")
                {
                    Content = new StringContent("test content", Encoding.UTF8, "text/plain")
                };
                request.Headers.Add("X-Custom-Header", "CustomValue");
                request.Headers.Add("X-Test-ID", "12345");

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.True(response.IsSuccessStatusCode);
                AssertLogContainsAll(
                    "Headers:",
                    "X-Custom-Header",
                    "CustomValue",
                    "X-Test-ID",
                    "12345"
                );
            }
        }
    }
}
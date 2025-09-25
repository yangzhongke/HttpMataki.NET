using System.Collections.Generic;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace HttpMataki.NET.IntegrationTests.Infrastructure
{
    /// <summary>
    ///     Helper class for setting up common WireMock response scenarios
    /// </summary>
    public static class TestDataHelper
    {
        /// <summary>
        ///     Setup basic POST endpoint that echoes request data
        /// </summary>
        public static void SetupPostEcho(WireMockServer server)
        {
            server
                .Given(Request.Create().WithPath("/post").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        args = new { },
                        data = "{{request.body}}",
                        files = new { },
                        form = new { },
                        headers = new
                        {
                            Host = "{{request.headers.Host}}",
                            ContentType = "{{request.headers.Content-Type}}"
                        },
                        json = "{{request.bodyAsJson}}",
                        origin = "127.0.0.1",
                        url = "{{request.url}}"
                    }));
        }

        /// <summary>
        ///     Setup image response endpoint
        /// </summary>
        public static void SetupImageResponse(WireMockServer server)
        {
            // Create a minimal JPEG header for testing
            var jpegHeader = new byte[]
            {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
                0x01, 0x01, 0x00, 0x48, 0x00, 0x48, 0x00, 0x00, 0xFF, 0xD9
            };

            server
                .Given(Request.Create().WithPath("/image/jpeg").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "image/jpeg")
                    .WithBody(jpegHeader));
        }

        /// <summary>
        ///     Setup JSON response endpoints for jsonplaceholder simulation
        /// </summary>
        public static void SetupJsonPlaceholderEndpoints(WireMockServer server)
        {
            // GET /posts/{id}
            server
                .Given(Request.Create().WithPath("/posts/*").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        id = 1,
                        userId = 1,
                        title = "Test Post Title",
                        body = "This is a test post body from WireMock simulation."
                    }));

            // POST /posts
            server
                .Given(Request.Create().WithPath("/posts").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(201)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        id = 101,
                        userId = "{{request.bodyAsJson.userId}}",
                        title = "{{request.bodyAsJson.title}}",
                        body = "{{request.bodyAsJson.body}}"
                    }));

            // GET /users
            server
                .Given(Request.Create().WithPath("/users").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new[]
                    {
                        new
                        {
                            id = 1,
                            name = "Test User 1",
                            email = "user1@test.com",
                            username = "testuser1"
                        },
                        new
                        {
                            id = 2,
                            name = "Test User 2",
                            email = "user2@test.com",
                            username = "testuser2"
                        }
                    }));

            // GET /users/{id}
            server
                .Given(Request.Create().WithPath("/users/*").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        id = 1,
                        name = "Test User",
                        email = "testuser@test.com",
                        username = "testuser"
                    }));
        }

        /// <summary>
        ///     Setup error response scenarios
        /// </summary>
        public static void SetupErrorResponses(WireMockServer server)
        {
            server
                .Given(Request.Create().WithPath("/error/404").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(404)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new { error = "Not Found", message = "The requested resource was not found." }));

            server
                .Given(Request.Create().WithPath("/error/500").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(500)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                        { error = "Internal Server Error", message = "An internal server error occurred." }));

            server
                .Given(Request.Create().WithPath("/timeout").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithDelay(5000) // 5 second delay to simulate timeout
                    .WithBodyAsJson(new { message = "This response was delayed" }));
        }

        /// <summary>
        ///     Setup various content type responses
        /// </summary>
        public static void SetupContentTypeResponses(WireMockServer server)
        {
            // XML response
            server
                .Given(Request.Create().WithPath("/xml").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/xml")
                    .WithBody("<?xml version=\"1.0\"?><test><message>XML response from WireMock</message></test>"));

            // Plain text response
            server
                .Given(Request.Create().WithPath("/text").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("Plain text response from WireMock server"));

            // GraphQL response
            server
                .Given(Request.Create().WithPath("/graphql").UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        data = new
                        {
                            user = new
                            {
                                id = "1",
                                name = "Test User",
                                email = "test@example.com"
                            }
                        }
                    }));

            // Large JSON response
            server
                .Given(Request.Create().WithPath("/large-json").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(GenerateLargeJsonResponse()));
        }

        /// <summary>
        ///     Generate a large JSON response for testing
        /// </summary>
        private static object GenerateLargeJsonResponse()
        {
            var items = new List<object>();
            for (var i = 1; i <= 100; i++)
            {
                items.Add(new
                {
                    id = i,
                    name = $"Item {i}",
                    description =
                        $"This is a test item number {i} with some additional content to make the response larger.",
                    category = $"Category {i % 10}",
                    price = i * 10.99m,
                    inStock = i % 2 == 0,
                    tags = new[] { $"tag{i}", $"test{i}", "wiremock" }
                });
            }

            return new { items, total = items.Count, page = 1, pageSize = 100 };
        }
    }
}
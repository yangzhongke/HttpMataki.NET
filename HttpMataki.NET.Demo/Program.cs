using System.Net.Http.Headers;
using System.Text;
using HttpMataki.NET;
using HttpMataki.NET.Auto;
using Microsoft.Extensions.DependencyInjection;
using Refit;

await BasicDemo1Async();
await RefitApiDemoAsync();
await HarmonyAutoInterceptionDemoAsync();

async Task BasicDemo1Async()
{
    var handler = new HttpLoggingHandler();
    using var client = new HttpClient(handler);

    // Text request
    Console.WriteLine("**********Text request**********");
    var textRequest = new HttpRequestMessage(HttpMethod.Post, "https://httpbin.org/post")
    {
        Content = new StringContent("Hello, this is plain text!", Encoding.UTF8, "text/plain")
    };
    await client.SendAsync(textRequest);

    // JSON request
    Console.WriteLine("**********JSON request**********");
    var jsonRequest = new HttpRequestMessage(HttpMethod.Post, "https://httpbin.org/post")
    {
        Content = new StringContent("{\"name\":\"Mataki\",\"type\":\"json\"}", Encoding.UTF8, "application/json")
    };
    await client.SendAsync(jsonRequest);

    // Image request (download and process image)
    Console.WriteLine("**********Image request (download and process image)**********");
    var imageRequest = new HttpRequestMessage(HttpMethod.Get, "https://httpbin.org/image/jpeg");
    await client.SendAsync(imageRequest);

    // File upload (multipart/form-data)
    Console.WriteLine("**********File upload**********");
    var multipartContent = new MultipartFormDataContent();
    var fileBytes = Encoding.UTF8.GetBytes("This is a demo file.");
    var fileContent = new ByteArrayContent(fileBytes);
    fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
    multipartContent.Add(fileContent, "file", "demo.txt");
    multipartContent.Add(new StringContent("field value"), "textField");
    var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "https://httpbin.org/post")
    {
        Content = multipartContent
    };
    await client.SendAsync(uploadRequest);

    // Form (application/x-www-form-urlencoded)
    var formData = new List<KeyValuePair<string, string>>
    {
        new("name", "Mataki"),
        new("email", "mataki@example.com"),
        new("age", "28")
    };
    var formContent = new FormUrlEncodedContent(formData);
    var formRequest = new HttpRequestMessage(HttpMethod.Post, "https://httpbin.org/post")
    {
        Content = formContent
    };
    await client.SendAsync(formRequest);
}

async Task RefitApiDemoAsync()
{
    Console.WriteLine("**********Refit API Client Demo**********");

    var handler = new HttpLoggingHandler();
    using var client = new HttpClient(handler)
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com")
    };

    var api = RestService.For<IJsonPlaceholderApi>(client);
    var newPost = new Post
    {
        UserId = 1,
        Title = "HttpMataki Demo Post",
        Body = "This post was created using Refit with HttpMataki logging."
    };
    await api.CreatePostAsync(newPost);
}

async Task HarmonyAutoInterceptionDemoAsync()
{
    Console.WriteLine("**********Harmony Auto-Interception Demo**********");
    Console.WriteLine("This demo shows how HttpMataki.NET.Auto automatically intercepts ALL HttpClient instances");
    Console.WriteLine();

    // Enable automatic interception
    HttpClientAutoInterceptor.StartInterception();

    try
    {
        Console.WriteLine("--- Test 1: Direct HttpClient creation ---");
        // This HttpClient will automatically have HttpLoggingHandler injected
        using var client1 = new HttpClient();
        await client1.GetAsync("https://jsonplaceholder.typicode.com/posts/1");

        Console.WriteLine("\n--- Test 2: HttpClient with custom handler ---");
        // Even with a custom handler, it will be wrapped with HttpLoggingHandler
        var customHandler = new HttpClientHandler { UseCookies = false };
        using var client2 = new HttpClient(customHandler);
        await client2.GetAsync("https://jsonplaceholder.typicode.com/posts/2");

        Console.WriteLine("\n--- Test 3: Third-party library (Refit) ---");
        // Refit creates HttpClient internally - it will also be intercepted
        using var client3 = new HttpClient { BaseAddress = new Uri("https://jsonplaceholder.typicode.com") };
        var api = RestService.For<IJsonPlaceholderApi>(client3);

        var newPost = new Post
        {
            UserId = 1,
            Title = "Auto-Intercepted Post",
            Body = "This request was automatically intercepted by HttpMataki.NET.Auto!"
        };
        await api.CreatePostAsync(newPost);

        Console.WriteLine("\n--- Test 4: HttpClientFactory ---");
        // HttpClientFactory also creates HttpClient instances that will be intercepted
        var services = new ServiceCollection();
        services.AddHttpClient("test");
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var client4 = factory.CreateClient("test");
        await client4.GetAsync("https://jsonplaceholder.typicode.com/posts/3");

        Console.WriteLine("\n--- Test 5: Simulating a sealed HTTP wrapper ---");
        // This simulates a third-party library that doesn't expose HttpClient configuration
        await SimulateThirdPartyLibraryAsync();
    }
    finally
    {
        // Clean up - disable automatic interception
        HttpClientAutoInterceptor.StopInterception();
        Console.WriteLine("\nAutomatic interception disabled.");
    }
}

// Simulates a third-party library that creates HttpClient internally
async Task SimulateThirdPartyLibraryAsync()
{
    Console.WriteLine("Simulating third-party library that creates HttpClient internally...");

    // This represents a library method that you can't modify
    // but it creates HttpClient internally - it will still be logged!
    await SomeThirdPartyApiWrapperAsync("https://jsonplaceholder.typicode.com/users");
}

// This simulates a method from a third-party library
async Task SomeThirdPartyApiWrapperAsync(string baseUrl)
{
    // This simulates what a third-party library might do:
    // Create HttpClient internally without exposing configuration
    using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

    // Make some requests
    var response = await httpClient.GetAsync("/1");
    var content = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"Third-party library got response: {content.Substring(0, Math.Min(100, content.Length))}...");
}

// API interface for JSONPlaceholder
public interface IJsonPlaceholderApi
{
    [Post("/posts")]
    Task<Post> CreatePostAsync([Body] Post post);
}

// Data models
public class Post
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
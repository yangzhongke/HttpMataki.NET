using System.Net.Http.Headers;
using HttpMataki.NET;
using System.Text;
using Refit;

await BasicDemo1Async();
await RefitApiDemoAsync();

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
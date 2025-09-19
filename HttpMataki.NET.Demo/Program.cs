using System.Net.Http;
using System.Net.Http.Headers;
using HttpMataki.NET;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("HttpMataki.NET 演示：");
        var handler = new HttpLoggingHandler();
        using var client = new HttpClient(handler);

        // 文本请求
        var textRequest = new HttpRequestMessage(HttpMethod.Post, "https://httpbin.org/post")
        {
            Content = new StringContent("Hello, this is plain text!", Encoding.UTF8, "text/plain")
        };
        await client.SendAsync(textRequest);

        // JSON 请求
        var jsonRequest = new HttpRequestMessage(HttpMethod.Post, "https://httpbin.org/post")
        {
            Content = new StringContent("{\"name\":\"Mataki\",\"type\":\"json\"}", Encoding.UTF8, "application/json")
        };
        await client.SendAsync(jsonRequest);

        // 图片请求（下载图片并处理）
        var imageRequest = new HttpRequestMessage(HttpMethod.Get, "https://httpbin.org/image/jpeg");
        await client.SendAsync(imageRequest);

        // 文件上传（multipart/form-data）
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

        // 表单（application/x-www-form-urlencoded）
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

        Console.WriteLine("演示结束。");
    }
}

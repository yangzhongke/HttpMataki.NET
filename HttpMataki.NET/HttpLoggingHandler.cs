using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;

namespace HttpMataki.NET;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly Action<string> _logAction;

    public HttpLoggingHandler()
    {
        _logAction = Console.WriteLine;
    }

    public HttpLoggingHandler(string logFilePath)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            throw new ArgumentException("Log file path cannot be null or empty.", nameof(logFilePath));
        }

        _logAction = message =>
        {
            File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
        };
    }

    public HttpLoggingHandler(Action<string> logAction)
    {
        _logAction = logAction ?? throw new ArgumentNullException(nameof(logAction));
    }

    private void WriteLine(string message)
    {
        _logAction($"{message}{Environment.NewLine}");
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Log request details
        WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Request:");
        WriteLine($"Method: {request.Method}");
        WriteLine($"URL: {request.RequestUri}");
        
        // Log request headers
        WriteLine("Headers:");
        foreach (var header in request.Headers)
        {
            WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        if (request.Content != null)
        {
            var requestMediaType = request.Content.Headers.ContentType?.MediaType;
            var requestCharset = request.Content.Headers.ContentType?.CharSet;
            var requestEncoding = EncodingHelper.GetEncodingFromContentType(requestCharset);
            
            // Log content headers
            foreach (var header in request.Content.Headers)
            {
                WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
            }

            if (string.IsNullOrWhiteSpace(requestMediaType))
            {
                WriteLine("Null or empty Content-Type header.");
            }
            else if (requestMediaType.StartsWith("multipart/form-data"))
            {
                await HandleMultipartContent(request.Content);
            }
            else if (IsTextMediaType(requestMediaType))
            {
                var requestBody = await request.Content.ReadAsStringAsync();
                WriteLine($"Body: {requestBody}");
                request.Content =
                    new StringContent(requestBody, requestEncoding, requestMediaType);
            }
            else
            {
                WriteLine($"Content-Type: {requestMediaType}");
            }
        }
        else
        {
            WriteLine($"Body: Empty Content");
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Log response details
        WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Response:");
        WriteLine($"Status Code: {(int)response.StatusCode} {response.StatusCode}");
        
        // Log response headers
        WriteLine("Headers:");
        foreach (var header in response.Headers)
        {
            WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        var respContentType = response.Content.Headers.ContentType;
        var respMediaType = respContentType?.MediaType;
        var respCharset = respContentType?.CharSet;
        var respEncoding = EncodingHelper.GetEncodingFromContentType(respCharset);

        // Log content headers
        foreach (var header in response.Content.Headers)
        {
            WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        if (respMediaType != null && (respMediaType.StartsWith("text/") || respMediaType == "application/json"))
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            WriteLine($"Body: {responseBody}");
            response.Content = new StringContent(responseBody, respEncoding, respMediaType);
        }
        else
        {
            WriteLine($"Content-Type: {respMediaType}");
        }
        WriteLine(new string('*', 50));
        return response;
    }

    private static bool IsTextMediaType(string requestMediaType)
    {
        return requestMediaType.StartsWith("text/") 
               || requestMediaType == "application/json" || requestMediaType.EndsWith("+json") 
               || requestMediaType == "application/xml"|| requestMediaType.EndsWith("+xml")
               || requestMediaType == "application/yaml"|| requestMediaType.EndsWith("+yaml")
               || requestMediaType == "application/graphql";
    }

    private async Task HandleMultipartContent(HttpContent content)
    {
        try
        {
            WriteLine("Multipart Form Data Content:");
            var provider = new MultipartMemoryStreamProvider();
            var multipartContent = await content.ReadAsMultipartAsync(provider);
            foreach (var part in multipartContent.Contents)
            {
                var contentDisposition = part.Headers.ContentDisposition;
                var contentType = part.Headers.ContentType?.MediaType;
                if (contentDisposition != null)
                {
                    var fieldName = contentDisposition.Name?.Trim('"');
                    var fileName = contentDisposition.FileName?.Trim('"');
                    WriteLine($"  Field: {fieldName}");
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        WriteLine($"  Original FileName: {fileName}");
                        WriteLine($"  Content-Type: {contentType}");
                        var tempDir = Path.Combine(Path.GetTempPath(), "HttpMataki_Uploads");
                        Directory.CreateDirectory(tempDir);
                        var tempFileName = $"{Guid.NewGuid()}_{fileName}";
                        var tempFilePath = Path.Combine(tempDir, tempFileName);
                        var fileBytes = await part.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(tempFilePath, fileBytes);
                        WriteLine($"  Saved to: {tempFilePath}");
                        WriteLine($"  File Size: {fileBytes.Length} bytes");
                    }
                    else
                    {
                        var fieldValue = await part.ReadAsStringAsync();
                        WriteLine($"  Value: {fieldValue}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Error processing multipart content: {ex.Message}");
        }
    }
}
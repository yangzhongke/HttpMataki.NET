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
                // Output raw body content
                var rawBody = await request.Content.ReadAsStringAsync();
                WriteLine($"Raw Body: {rawBody}");
                request.Content = new StringContent(rawBody, requestEncoding);
            }
            else if (requestMediaType.StartsWith("multipart/form-data"))
            {
                await HandleMultipartContent(request.Content);
            }
            else if (requestMediaType == "application/x-www-form-urlencoded")
            {
                await HandleUrlEncodedContent(request.Content);
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
                // Output raw body content for unknown media types
                var rawBody = await request.Content.ReadAsStringAsync();
                WriteLine($"Raw Body: {rawBody}");
                request.Content = new StringContent(rawBody, requestEncoding, requestMediaType);
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

        if(string.IsNullOrWhiteSpace(respMediaType))
        {
            WriteLine("Null or empty Content-Type header.");
            // Output raw body content
            var rawBody = await response.Content.ReadAsStringAsync();
            WriteLine($"Raw Body: {rawBody}");
            response.Content = new StringContent(rawBody, respEncoding);
        }
        else if (IsImageMediaType(respMediaType))
        {
            await HandleImageResponse(response, respMediaType);
        }
        else if (IsTextMediaType(respMediaType))
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            WriteLine($"Body: {responseBody}");
            response.Content = new StringContent(responseBody, respEncoding, respMediaType);
        }
        else
        {
            WriteLine($"Content-Type: {respMediaType}");
            var rawBody = await response.Content.ReadAsStringAsync();
            WriteLine($"Raw Body: {rawBody}");
            response.Content = new StringContent(rawBody, respEncoding, respMediaType);
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

    private static bool IsImageMediaType(string mediaType)
    {
        return mediaType.StartsWith("image/");
    }

    private async Task HandleImageResponse(HttpResponseMessage response, string mediaType)
    {
        try
        {
            WriteLine($"Image Content Type: {mediaType}");
            
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            WriteLine($"Image Size: {imageBytes.Length} bytes");
            
            // Create temp directory for images
            var tempDir = Path.Combine(Path.GetTempPath(), "HttpMataki_Images");
            Directory.CreateDirectory(tempDir);
            
            // Generate file name with appropriate extension
            var extension = GetImageExtension(mediaType);
            var tempFileName = $"{Guid.NewGuid()}{extension}";
            var tempFilePath = Path.Combine(tempDir, tempFileName);
            
            // Save image to temp file
            await File.WriteAllBytesAsync(tempFilePath, imageBytes);
            WriteLine($"Image saved to: {tempFilePath}");
            
            // Recreate content to preserve the response
            response.Content = new ByteArrayContent(imageBytes);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
        }
        catch (Exception ex)
        {
            WriteLine($"Error processing image response: {ex.Message}");
        }
    }

    private static string GetImageExtension(string mediaType)
    {
        return mediaType.ToLower() switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "image/tiff" => ".tiff",
            "image/ico" => ".ico",
            "image/x-icon" => ".ico",
            _ => ".img"
        };
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

    private async Task HandleUrlEncodedContent(HttpContent content)
    {
        try
        {
            WriteLine("Form URL Encoded Content:");
            var formData = await content.ReadAsStringAsync();
            WriteLine($"Raw Data: {formData}");
            
            if (!string.IsNullOrEmpty(formData))
            {
                WriteLine("Parsed Form Fields:");
                var pairs = formData.Split('&');
                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(keyValue[0].Replace('+', ' '));
                        var value = Uri.UnescapeDataString(keyValue[1].Replace('+', ' '));
                        WriteLine($"  {key}: {value}");
                    }
                    else if (keyValue.Length == 1)
                    {
                        var key = Uri.UnescapeDataString(keyValue[0].Replace('+', ' '));
                        WriteLine($"  {key}: (no value)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            WriteLine($"Error processing URL encoded content: {ex.Message}");
        }
    }
}
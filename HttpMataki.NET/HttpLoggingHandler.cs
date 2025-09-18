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
        if (request.Content != null)
        {
            var requestMediaType = request.Content.Headers.ContentType?.MediaType;
            var requestCharset = request.Content.Headers.ContentType?.CharSet;
            var requestEncoding = EncodingHelper.GetEncodingFromContentType(requestCharset);
            WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Request:");
            if (requestMediaType != null &&
                (requestMediaType.StartsWith("text/") || requestMediaType == "application/json"))
            {
                var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
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

        var respContentType = response.Content.Headers.ContentType;
        var respMediaType = respContentType?.MediaType;
        var respCharset = respContentType?.CharSet;
        var respEncoding = EncodingHelper.GetEncodingFromContentType(respCharset);

        WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Response:");
        if (respMediaType != null && (respMediaType.StartsWith("text/") || respMediaType == "application/json"))
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            WriteLine($"Body: {responseBody}");
            response.Content = new StringContent(responseBody, respEncoding, respMediaType);
        }
        else
        {
            WriteLine($"Content-Type: {respMediaType}");
        }

        return response;
    }
}
namespace HttpMataki.NET;

/// <summary>
/// Represents an HTTP request captured by the observer
/// </summary>
public class HttpRequestData
{
    public string Method { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public Dictionary<string, string[]> Headers { get; set; } = new();
    public string? Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents an HTTP response captured by the observer
/// </summary>
public class HttpResponseData
{
    public int StatusCode { get; set; }
    public string ReasonPhrase { get; set; } = string.Empty;
    public Dictionary<string, string[]> Headers { get; set; } = new();
    public string? Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a complete HTTP communication record
/// </summary>
public class HttpCommunicationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public HttpRequestData Request { get; set; } = new();
    public HttpResponseData? Response { get; set; }
    public TimeSpan? Duration { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Interface for HTTP communication observers
/// </summary>
public interface IHttpObserver
{
    /// <summary>
    /// Called when an HTTP communication is recorded
    /// </summary>
    /// <param name="record">The HTTP communication record</param>
    Task OnHttpCommunicationAsync(HttpCommunicationRecord record);
}

/// <summary>
/// Default HTTP observer that stores communications in memory
/// </summary>
public class InMemoryHttpObserver : IHttpObserver
{
    private readonly List<HttpCommunicationRecord> _records = new();
    private readonly object _lock = new();

    public async Task OnHttpCommunicationAsync(HttpCommunicationRecord record)
    {
        lock (_lock)
        {
            _records.Add(record);
        }
        await Task.CompletedTask;
    }

    public IReadOnlyList<HttpCommunicationRecord> GetRecords()
    {
        lock (_lock)
        {
            return _records.ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _records.Clear();
        }
    }
}

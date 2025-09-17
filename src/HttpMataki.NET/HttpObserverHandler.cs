using System.Diagnostics;
using System.Text;

namespace HttpMataki.NET;

/// <summary>
/// A DelegatingHandler that observes HTTP communications without disrupting program execution
/// </summary>
public class HttpObserverHandler : DelegatingHandler
{
    private readonly IHttpObserver _observer;

    public HttpObserverHandler(IHttpObserver observer)
    {
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var record = new HttpCommunicationRecord();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Capture request data
            await CaptureRequestAsync(request, record.Request);
            
            // Send the actual request
            var response = await base.SendAsync(request, cancellationToken);
            
            stopwatch.Stop();
            record.Duration = stopwatch.Elapsed;
            
            // Capture response data
            record.Response = new HttpResponseData();
            await CaptureResponseAsync(response, record.Response);
            
            // Notify observer 
            await _observer.OnHttpCommunicationAsync(record);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            record.Duration = stopwatch.Elapsed;
            record.Exception = ex;
            
            // Notify observer even for exceptions
            try
            {
                await _observer.OnHttpCommunicationAsync(record);
            }
            catch
            {
                // Silently ignore observer errors
            }
            
            throw;
        }
    }

    private static async Task CaptureRequestAsync(HttpRequestMessage request, HttpRequestData requestData)
    {
        requestData.Method = request.Method.Method;
        requestData.Uri = request.RequestUri?.ToString() ?? string.Empty;
        requestData.Timestamp = DateTime.UtcNow;
        
        // Capture headers
        foreach (var header in request.Headers)
        {
            requestData.Headers[header.Key] = header.Value.ToArray();
        }
        
        if (request.Content != null)
        {
            // Capture content headers
            foreach (var header in request.Content.Headers)
            {
                requestData.Headers[header.Key] = header.Value.ToArray();
            }
            
            // Capture content body (be careful with large payloads)
            try
            {
                requestData.Content = await request.Content.ReadAsStringAsync();
            }
            catch
            {
                requestData.Content = "[Unable to read content]";
            }
        }
    }

    private static async Task CaptureResponseAsync(HttpResponseMessage response, HttpResponseData responseData)
    {
        responseData.StatusCode = (int)response.StatusCode;
        responseData.ReasonPhrase = response.ReasonPhrase ?? string.Empty;
        responseData.Timestamp = DateTime.UtcNow;
        
        // Capture headers
        foreach (var header in response.Headers)
        {
            responseData.Headers[header.Key] = header.Value.ToArray();
        }
        
        if (response.Content != null)
        {
            // Capture content headers
            foreach (var header in response.Content.Headers)
            {
                responseData.Headers[header.Key] = header.Value.ToArray();
            }
            
            // Capture content body (be careful with large payloads)
            try
            {
                responseData.Content = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                responseData.Content = "[Unable to read content]";
            }
        }
    }
}
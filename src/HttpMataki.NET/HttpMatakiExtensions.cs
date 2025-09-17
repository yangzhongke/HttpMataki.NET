using Microsoft.Extensions.DependencyInjection;

namespace HttpMataki.NET;

/// <summary>
/// Extension methods for easy integration with HttpClient and DI container
/// </summary>
public static class HttpMatakiExtensions
{
    /// <summary>
    /// Adds HttpMataki observer to the HttpClient
    /// </summary>
    /// <param name="client">The HttpClient to observe</param>
    /// <param name="observer">The observer to use</param>
    /// <returns>The HttpClient for chaining</returns>
    public static HttpClient WithObserver(this HttpClient client, IHttpObserver observer)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        
        // Note: This method is primarily for demonstration and testing
        // In production, it's better to configure the handler during HttpClient creation
        throw new NotSupportedException(
            "Adding observer to existing HttpClient is not supported. " +
            "Use HttpClientFactory or configure the handler during HttpClient creation.");
    }

    /// <summary>
    /// Creates an HttpClient with HttpMataki observer
    /// </summary>
    /// <param name="observer">The observer to use</param>
    /// <param name="innerHandler">Optional inner handler (uses HttpClientHandler if null)</param>
    /// <returns>HttpClient with observer configured</returns>
    public static HttpClient CreateObservedHttpClient(IHttpObserver observer, HttpMessageHandler? innerHandler = null)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        
        var handler = new HttpObserverHandler(observer);
        if (innerHandler != null)
        {
            handler.InnerHandler = innerHandler;
        }
        else
        {
            handler.InnerHandler = new HttpClientHandler();
        }
        
        return new HttpClient(handler);
    }

    /// <summary>
    /// Adds HttpMataki services to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHttpMataki(this IServiceCollection services)
    {
        services.AddSingleton<IHttpObserver, InMemoryHttpObserver>();
        services.AddTransient<HttpObserverHandler>();
        
        return services;
    }

    /// <summary>
    /// Adds HttpMataki services with a custom observer to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="observer">The custom observer instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHttpMataki(this IServiceCollection services, IHttpObserver observer)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));
        
        services.AddSingleton(observer);
        services.AddTransient<HttpObserverHandler>();
        
        return services;
    }

    /// <summary>
    /// Adds HttpMataki services with a custom observer factory to the DI container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="observerFactory">Factory function to create the observer</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddHttpMataki(this IServiceCollection services, Func<IServiceProvider, IHttpObserver> observerFactory)
    {
        if (observerFactory == null) throw new ArgumentNullException(nameof(observerFactory));
        
        services.AddSingleton(observerFactory);
        services.AddTransient<HttpObserverHandler>();
        
        return services;
    }
}
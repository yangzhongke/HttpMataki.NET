using System.Net.Http;
using HarmonyLib;

namespace HttpMataki.NET.Auto;

/// <summary>
///     Provides automatic HttpClient interception using Harmony to inject HttpLoggingHandler
///     into all HttpClient instances created in the application.
/// </summary>
public static class HttpClientAutoInterceptor
{
    private static Harmony? _harmony;
    private static readonly object _lockObject = new();
    private static Func<HttpLoggingHandler>? _handlerFactory;

    /// <summary>
    ///     Gets whether automatic interception is currently active.
    /// </summary>
    public static bool IsInterceptionActive { get; private set; }

    /// <summary>
    ///     Starts automatic interception of HttpClient constructors.
    ///     All HttpClient instances will automatically include HttpLoggingHandler.
    /// </summary>
    /// <param name="handlerFactory">Optional factory function to create custom HttpLoggingHandler instances</param>
    public static void StartInterception(Func<HttpLoggingHandler>? handlerFactory = null)
    {
        lock (_lockObject)
        {
            if (IsInterceptionActive)
            {
                return;
            }

            _handlerFactory = handlerFactory ?? (() => new HttpLoggingHandler());
            _harmony = new Harmony("HttpMataki.NET.Auto.HttpClient.Patch");

            PatchHttpClientConstructors();
            IsInterceptionActive = true;
        }
    }

    /// <summary>
    ///     Stops automatic interception and removes all Harmony patches.
    /// </summary>
    public static void StopInterception()
    {
        lock (_lockObject)
        {
            if (!IsInterceptionActive || _harmony == null)
            {
                return;
            }

            _harmony.UnpatchAll("HttpMataki.NET.Auto.HttpClient.Patch");
            _harmony = null;
            _handlerFactory = null;
            IsInterceptionActive = false;

            Console.WriteLine("[HttpMataki.NET.Auto] HttpClient automatic interception stopped");
        }
    }

    private static void PatchHttpClientConstructors()
    {
        var httpClientType = typeof(HttpClient);

        // Patch HttpClient(HttpMessageHandler handler, bool disposeHandler) constructor
        var fullConstructor = httpClientType.GetConstructor(new[] { typeof(HttpMessageHandler), typeof(bool) });
        if (fullConstructor != null)
        {
            var prefix =
                typeof(HttpClientConstructorPatches).GetMethod(nameof(HttpClientConstructorPatches
                    .HttpClientFullConstructorPrefix));
            _harmony!.Patch(fullConstructor, new HarmonyMethod(prefix));
        }
    }

    internal static HttpLoggingHandler CreateHandler(HttpMessageHandler? innerHandler = null)
    {
        var handler = _handlerFactory?.Invoke() ?? new HttpLoggingHandler();

        if (innerHandler != null && handler.InnerHandler == null)
        {
            // If the handler doesn't have an inner handler set, try to set it
            var innerHandlerProperty = typeof(DelegatingHandler).GetProperty("InnerHandler");
            if (innerHandlerProperty?.CanWrite == true)
            {
                innerHandlerProperty.SetValue(handler, innerHandler);
            }
        }

        return handler;
    }
}

/// <summary>
///     Contains Harmony patch methods for HttpClient constructors.
/// </summary>
internal static class HttpClientConstructorPatches
{
    /// <summary>
    ///     Harmony prefix patch for HttpClient(HttpMessageHandler handler, bool disposeHandler) constructor.
    /// </summary>
    /// <param name="handler">The HttpMessageHandler being passed to the constructor</param>
    /// <param name="disposeHandler">Whether to dispose the handler</param>
    public static void HttpClientFullConstructorPrefix(ref HttpMessageHandler handler, bool disposeHandler)
    {
        // Only wrap if it's not already a HttpLoggingHandler
        if (handler is not HttpLoggingHandler)
        {
            handler = HttpClientAutoInterceptor.CreateHandler(handler);
        }
    }
}
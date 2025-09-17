using Microsoft.Extensions.DependencyInjection;

namespace HttpMataki.NET.UnitTests;

public class HttpMatakiExtensionsTests
{
    [Fact]
    public void CreateObservedHttpClient_WithNullObserver_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => HttpMatakiExtensions.CreateObservedHttpClient(null!));
    }

    [Fact]
    public void CreateObservedHttpClient_WithValidObserver_ReturnsHttpClient()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();

        // Act
        var client = HttpMatakiExtensions.CreateObservedHttpClient(observer);

        // Assert
        Assert.NotNull(client);
        client.Dispose();
    }

    [Fact]
    public void CreateObservedHttpClient_WithInnerHandler_UsesProvidedHandler()
    {
        // Arrange
        var observer = new InMemoryHttpObserver();
        var innerHandler = new HttpClientHandler();

        // Act
        var client = HttpMatakiExtensions.CreateObservedHttpClient(observer, innerHandler);

        // Assert
        Assert.NotNull(client);
        client.Dispose();
        innerHandler.Dispose();
    }

    [Fact]
    public void AddHttpMataki_WithoutParameters_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHttpMataki();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var observer = serviceProvider.GetService<IHttpObserver>();
        var handler = serviceProvider.GetService<HttpObserverHandler>();

        Assert.NotNull(observer);
        Assert.IsType<InMemoryHttpObserver>(observer);
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddHttpMataki_WithCustomObserver_RegistersCustomObserver()
    {
        // Arrange
        var services = new ServiceCollection();
        var customObserver = new CustomTestObserver();

        // Act
        services.AddHttpMataki(customObserver);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var observer = serviceProvider.GetService<IHttpObserver>();
        var handler = serviceProvider.GetService<HttpObserverHandler>();

        Assert.NotNull(observer);
        Assert.Same(customObserver, observer);
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddHttpMataki_WithNullObserver_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddHttpMataki((IHttpObserver)null!));
    }

    [Fact]
    public void AddHttpMataki_WithFactory_RegistersFactoryCreatedObserver()
    {
        // Arrange
        var services = new ServiceCollection();
        var factoryObserver = new CustomTestObserver();

        // Act
        services.AddHttpMataki(_ => factoryObserver);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var observer = serviceProvider.GetService<IHttpObserver>();
        var handler = serviceProvider.GetService<HttpObserverHandler>();

        Assert.NotNull(observer);
        Assert.Same(factoryObserver, observer);
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddHttpMataki_WithNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddHttpMataki((Func<IServiceProvider, IHttpObserver>)null!));
    }

    [Fact]
    public void WithObserver_ThrowsNotSupportedException()
    {
        // Arrange
        var client = new HttpClient();
        var observer = new InMemoryHttpObserver();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => client.WithObserver(observer));
        
        client.Dispose();
    }

    private class CustomTestObserver : IHttpObserver
    {
        public Task OnHttpCommunicationAsync(HttpCommunicationRecord record)
        {
            return Task.CompletedTask;
        }
    }
}
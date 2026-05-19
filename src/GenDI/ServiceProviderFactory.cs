using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Custom ServiceProviderFactory for HostBuilder integration.
/// Wraps the default provider with GenDI decorators.
/// </summary>
internal sealed class ServiceProviderFactory(Dictionary<Type, List<(ServiceLifetime lifetime, object? key, Func<IServiceProvider, object?, object> factory)>> decorators) : IServiceProviderFactory<IServiceCollection>
{
    private readonly Dictionary<Type, List<(ServiceLifetime lifetime, object? key, Func<IServiceProvider, object?, object> factory)>> _decorators = decorators;

    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        var innerProvider = services.BuildServiceProvider();
        return new ServiceProvider(innerProvider, _decorators);
    }
}

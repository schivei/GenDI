using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
namespace GenDI;

/// <summary>
/// Custom ServiceProvider that applies decorators and caches instances per lifetime.
/// </summary>
[ExcludeFromCodeCoverage]
internal class ServiceProvider(IServiceProvider innerProvider,
    Dictionary<Type, List<(ServiceLifetime, object? key, Func<IServiceProvider, object?, object> factory)>> decorators
) : IServiceScopeFactory, IKeyedServiceProvider
{
    private readonly Dictionary<Type, List<(ServiceLifetime lifetime, object? key, Func<IServiceProvider, object?, object> factory)>> _decorators = decorators;
    private readonly Dictionary<Type, Lazy<object>> _singletonCache = [];

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        if (_singletonCache.TryGetValue(serviceType, out var cached))
            return cached.Value;

        var original = serviceKey is null ? innerProvider.GetService(serviceType) : innerProvider.GetKeyedService(serviceType, serviceKey);

        if (_decorators.TryGetValue(serviceType, out var factories))
        {
            return ServiceFactory(serviceType, [.. factories.Where(f => f.key == serviceKey)], original);
        }

        return original;
    }

    public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
        GetKeyedService(serviceType, serviceKey) ??
        throw new InvalidOperationException($"No service of type '{serviceType}' with key '{serviceKey}' is registered.");

    public object GetService(Type serviceType) =>
        GetKeyedService(serviceType, null);

    private object? ServiceFactory(Type serviceType, List<(ServiceLifetime lifetime, object? key, Func<IServiceProvider, object?, object> factory)> factories, object? original)
    {
        var current = original;

        foreach (var (lifetime, key, factory) in factories)
        {
            if (lifetime is not ServiceLifetime.Singleton)
            {
                current = instance();
                continue;
            }

            _singletonCache[serviceType] = new Lazy<object>(instance);

            current = _singletonCache[serviceType].Value;

            object instance() => factory(new DecoratorContext(innerProvider, current), key);
        }

        return current;
    }

    public IServiceScope CreateScope() =>
        new ServiceScope(innerProvider.CreateScope(), _decorators);

    [ExcludeFromCodeCoverage]
    private sealed class DecoratorContext(IServiceProvider inner, object current) : IKeyedServiceProvider
    {
        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            if (current is { } && serviceType.IsInstanceOfType(current))
                return current;

            return inner.GetKeyedService(serviceType, serviceKey);
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
            GetKeyedService(serviceType, serviceKey) ??
            throw new InvalidOperationException($"No service of type '{serviceType}' with key '{serviceKey}' is registered.");

        public object GetService(Type serviceType)
        {
            if (current is { } && serviceType.IsInstanceOfType(current))
                return current;

            return inner.GetService(serviceType);
        }
    }
}

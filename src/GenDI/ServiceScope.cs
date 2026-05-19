using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace GenDI;

[ExcludeFromCodeCoverage]
internal sealed class ServiceScope(IServiceScope innerScope,
    Dictionary<Type, List<(ServiceLifetime, object?, Func<IServiceProvider, object?, object>)>> decorators) : IServiceScope
{
    private bool _disposed;
    private readonly Dictionary<Type, List<(ServiceLifetime lifetime, object? key, Func<IServiceProvider, object?, object> factory)>> _decorators = decorators;
    private readonly Dictionary<Type, object> _scopedCache = [];

    public IServiceProvider ServiceProvider => new ScopedProvider(innerScope.ServiceProvider, _decorators, _scopedCache);

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            innerScope.Dispose();
            _disposed = true;
        }
    }

    ~ServiceScope()
    {
        Dispose(false);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ScopedProvider(IServiceProvider inner,
        Dictionary<Type, List<(ServiceLifetime, object? key, Func<IServiceProvider, object?, object> factory)>> decorators,
        Dictionary<Type, object> scopedCache) : IKeyedServiceProvider
    {
        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            if (scopedCache.TryGetValue(serviceType, out var cached))
                return cached;

            var original = inner.GetService(serviceType);
            if (original == null)
                return null;

            if (decorators.TryGetValue(serviceType, out var factories))
            {
                object current = original;
                foreach (var (lifetime, key, factory) in factories)
                {
                    var instance = factory(new DecoratorContext(inner, current), key);
                    if (lifetime == ServiceLifetime.Scoped)
                        scopedCache[serviceType] = instance;

                    current = instance;
                }

                return current;
            }

            return original;
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey) =>
            GetKeyedService(serviceType, serviceKey) ??
            throw new InvalidOperationException($"No service of type '{serviceType}' with key '{serviceKey}' is registered.");

        public object GetService(Type serviceType) =>
            GetKeyedService(serviceType, null);

        [ExcludeFromCodeCoverage]
        private sealed class DecoratorContext(IServiceProvider inner, object current) : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                if (serviceType.IsInstanceOfType(current))
                    return current;

                return inner.GetService(serviceType);
            }
        }
    }
}

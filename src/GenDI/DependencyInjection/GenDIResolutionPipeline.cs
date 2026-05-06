using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace GenDI.DependencyInjection;

public static class GenDIResolutionPipeline
{
    private static readonly ConcurrentDictionary<Type, ServiceLifetime> LifetimeMap = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> GeneratedFactories = new();
    private static readonly ConcurrentDictionary<Type, object> SingletonCache = new();
    private static readonly ConditionalWeakTable<IServiceProvider, ConcurrentDictionary<Type, object>> ScopedCache = new();

    public static void RegisterGeneratedFactory(
        Type implementationType,
        ServiceLifetime lifetime,
        Func<IServiceProvider, object> factory)
    {
        LifetimeMap[implementationType] = lifetime;
        GeneratedFactories[implementationType] = factory;
    }

    public static object? ResolveOrFallback(IServiceProvider provider, Type serviceType)
    {
        if (GeneratedFactories.TryGetValue(serviceType, out var generatedFactory))
        {
            return ResolveFromFactory(provider, serviceType, generatedFactory);
        }

        return provider.GetService(serviceType);
    }

    public static object ResolveOrCreate(IServiceProvider provider, Type implementationType)
    {
        if (GeneratedFactories.TryGetValue(implementationType, out var generatedFactory))
        {
            return ResolveFromFactory(provider, implementationType, generatedFactory);
        }

        var fromProvider = provider.GetService(implementationType);
        if (fromProvider is not null)
        {
            return fromProvider;
        }

        throw new InvalidOperationException($"Unable to resolve '{implementationType.FullName}' using GenDI or IServiceProvider.");
    }

    private static object ResolveFromFactory(
        IServiceProvider provider,
        Type implementationType,
        Func<IServiceProvider, object> factory)
    {
        var lifetime = LifetimeMap.TryGetValue(implementationType, out var mappedLifetime)
            ? mappedLifetime
            : ServiceLifetime.Transient;

        return lifetime switch
        {
            ServiceLifetime.Singleton => SingletonCache.GetOrAdd(implementationType, _ => factory(provider)),
            ServiceLifetime.Scoped => ScopedCache.GetValue(provider, static _ => new ConcurrentDictionary<Type, object>())
                .GetOrAdd(implementationType, _ => factory(provider)),
            _ => factory(provider)
        };
    }
}

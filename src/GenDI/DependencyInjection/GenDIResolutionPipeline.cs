using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace GenDI.DependencyInjection;

public static class GenDIResolutionPipeline
{
    private static readonly ConcurrentDictionary<Type, ServiceLifetime> LifetimeMap = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> GeneratedFactories = new();
    private static readonly ConcurrentDictionary<Type, Lazy<object>> SingletonCache = new();
    private static readonly ConditionalWeakTable<IServiceProvider, ConcurrentDictionary<Type, Lazy<object>>> ScopedCache = new();

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

    public static object ResolveRequiredOrFallback(IServiceProvider provider, Type serviceType)
    {
        var resolved = ResolveOrFallback(provider, serviceType);
        if (resolved is not null)
        {
            return resolved;
        }

        throw new InvalidOperationException($"Unable to resolve dependency '{serviceType.FullName}' using GenDI or IServiceProvider.");
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
            ServiceLifetime.Singleton => SingletonCache.GetOrAdd(
                implementationType,
                _ => new Lazy<object>(() => factory(provider), LazyThreadSafetyMode.ExecutionAndPublication)).Value,
            ServiceLifetime.Scoped => ScopedCache.GetValue(provider, static _ => new ConcurrentDictionary<Type, Lazy<object>>())
                .GetOrAdd(
                    implementationType,
                    _ => new Lazy<object>(() => factory(provider), LazyThreadSafetyMode.ExecutionAndPublication))
                .Value,
            _ => factory(provider)
        };
    }
}

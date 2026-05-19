using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace GenDI;

/// <summary>
/// Provides extension methods for registering decorators in the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DecoratorExtensions
{
    private static readonly Dictionary<Type, List<(ServiceLifetime lifetime, object? key, Func<IServiceProvider, object?, object> factory)>> _decorators = [];

    /// <summary>
    /// Registers a Singleton decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="services">The IServiceCollection to add the decorator to.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddDecoratorSingleton<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class =>
        AddKeyedDecoratorSingleton(services, null, (sp, _) => implementationFactory(sp));

    /// <summary>
    /// Registers a Singleton decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="services">The IServiceCollection to add the decorator to.</param>
    /// <param name="key">An optional key to associate with the decorator, allowing for multiple decorators of the same service type.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddKeyedDecoratorSingleton<TService>(
        this IServiceCollection services,
        object? key,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class
    {
        RegisterDecorator(key, ServiceLifetime.Singleton, (sp, svcKey) => implementationFactory(sp, svcKey));
        return services;
    }

    /// <summary>
    /// Registers a Scoped decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="services">The IServiceCollection to add the decorator to.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddDecoratorScoped<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class =>
        AddKeyedDecoratorScoped(services, null, (sp, _) => implementationFactory(sp));

    /// <summary>
    /// Registers a Scoped decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="services">The IServiceCollection to add the decorator to.</param>
    /// <param name="key">An optional key to associate with the decorator, allowing for multiple decorators of the same service type.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddKeyedDecoratorScoped<TService>(
        this IServiceCollection services,
        object? key,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class
    {
        RegisterDecorator(key, ServiceLifetime.Scoped, (sp, svcKey) => implementationFactory(sp, svcKey));
        return services;
    }

    /// <summary>
    /// Registers a Transient decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="services">The IServiceCollection to add the decorator to.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddDecoratorTransient<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class =>
        AddKeyedDecoratorTransient(services, null, (sp, _) => implementationFactory(sp));


    /// <summary>
    /// Registers a Transient decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="services">The IServiceCollection to add the decorator to.</param>
    /// <param name="key">An optional key to associate with the decorator, allowing for multiple decorators of the same service type.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddKeyedDecoratorTransient<TService>(
        this IServiceCollection services,
        object? key,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class
    {
        RegisterDecorator(key, ServiceLifetime.Transient, implementationFactory);
        return services;
    }

    private static void RegisterDecorator<TService>(
        object? key,
        ServiceLifetime lifetime,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class
    {
        if (!_decorators.ContainsKey(typeof(TService)))
            _decorators[typeof(TService)] = [];

        _decorators[typeof(TService)].Add((lifetime, key, (sp, svcKey) => implementationFactory(sp, svcKey)));
    }

    /// <summary>
    /// Replaces the default ServiceProvider with GenDI in HostBuilder.
    /// </summary>
    /// <param name="builder">The IHostBuilder instance to configure.</param>
    /// <returns>The same IHostBuilder instance for chaining.</returns>
    public static IHostBuilder UseGenDI(this IHostBuilder builder)
    {
        return builder.UseServiceProviderFactory(new ServiceProviderFactory(_decorators));
    }

    /// <summary>
    /// Replaces the default ServiceProvider with GenDI in a generic IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to configure.</param>
    /// <returns>The configured IServiceProvider.</returns>
    public static IServiceProvider UseGenDI(this IServiceCollection services)
    {
        var factory = new ServiceProviderFactory(_decorators);
        var builder = factory.CreateBuilder(services);
        return factory.CreateServiceProvider(builder);
    }
}

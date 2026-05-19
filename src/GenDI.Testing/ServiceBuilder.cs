using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace GenDI.Testing;

/// <summary>
/// Provides a fluent service-collection builder for unit and integration tests.
/// </summary>

[ExcludeFromCodeCoverage]
public sealed class ServiceBuilder
{
    private readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBuilder"/> class.
    /// </summary>
    public ServiceBuilder()
        : this(new ServiceCollection()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBuilder"/> class with an existing collection.
    /// </summary>
    /// <param name="services">Service collection used by the builder.</param>
    public ServiceBuilder(IServiceCollection services)
    {
        ThrowIfNull(services, nameof(services));
        this._services = services;
    }

    /// <summary>
    /// Gets the backing service collection.
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Creates a new <see cref="ServiceBuilder"/> instance.
    /// </summary>
    /// <returns>A new service builder.</returns>
    public static ServiceBuilder Create() => new();

    /// <summary>
    /// Applies arbitrary service registration logic.
    /// </summary>
    /// <param name="configure">Delegate that configures <see cref="Services"/>.</param>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public ServiceBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        ThrowIfNull(configure, nameof(configure));
        configure(_services);
        return this;
    }

    /// <summary>
    /// Applies generated GenDI registration delegate (for example, <c>services.AddGenDIServices()</c>).
    /// </summary>
    /// <param name="addGenDiServices">Delegate that invokes generated GenDI registration.</param>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public ServiceBuilder AddGenDi(Action<IServiceCollection> addGenDiServices)
    {
        ThrowIfNull(addGenDiServices, nameof(addGenDiServices));
        addGenDiServices(_services);
        return this;
    }

    /// <summary>
    /// Adds a singleton registration to the service collection.
    /// </summary>
    /// <typeparam name="TService">Service contract type.</typeparam>
    /// <typeparam name="TImplementation">Concrete implementation type.</typeparam>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public ServiceBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddSingleton<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service instance.
    /// </summary>
    /// <typeparam name="TService">Service contract type.</typeparam>
    /// <param name="instance">Singleton instance.</param>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public ServiceBuilder AddSingleton<TService>(TService instance)
        where TService : class
    {
        ThrowIfNull(instance, nameof(instance));
        _services.AddSingleton(instance);
        return this;
    }

    /// <summary>
    /// Adds a scoped registration to the service collection.
    /// </summary>
    /// <typeparam name="TService">Service contract type.</typeparam>
    /// <typeparam name="TImplementation">Concrete implementation type.</typeparam>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public ServiceBuilder AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddScoped<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a transient registration to the service collection.
    /// </summary>
    /// <typeparam name="TService">Service contract type.</typeparam>
    /// <typeparam name="TImplementation">Concrete implementation type.</typeparam>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public ServiceBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        _services.AddTransient<TService, TImplementation>();
        return this;
    }

    /// <summary>
    /// Registers a Singleton decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public IServiceCollection AddDecoratorSingleton<TService>(
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class =>
        AddKeyedDecoratorSingleton(null, (sp, _) => implementationFactory(sp));

    /// <summary>
    /// Registers a Singleton decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="key">An optional key to associate with the decorator, allowing for multiple decorators of the same service type.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public IServiceCollection AddKeyedDecoratorSingleton<TService>(
        object? key,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class =>
        _services.AddKeyedDecoratorSingleton(key, implementationFactory);

    /// <summary>
    /// Registers a Scoped decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public IServiceCollection AddDecoratorScoped<TService>(
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class =>
        AddKeyedDecoratorScoped(null, (sp, _) => implementationFactory(sp));

    /// <summary>
    /// Registers a Scoped decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="key">An optional key to associate with the decorator, allowing for multiple decorators of the same service type.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public IServiceCollection AddKeyedDecoratorScoped<TService>(
        object? key,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class =>
        _services.AddKeyedDecoratorScoped(key, implementationFactory);

    /// <summary>
    /// Registers a Transient decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public IServiceCollection AddDecoratorTransient<TService>(
        Func<IServiceProvider, TService> implementationFactory)
        where TService : class =>
        AddKeyedDecoratorTransient(null, (sp, _) => implementationFactory(sp));


    /// <summary>
    /// Registers a Transient decorator for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service type to decorate.</typeparam>
    /// <param name="key">An optional key to associate with the decorator, allowing for multiple decorators of the same service type.</param>
    /// <param name="implementationFactory">
    /// A factory function that receives the current IServiceProvider and returns the decorated instance of <typeparamref name="TService"/>.
    /// </param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public IServiceCollection AddKeyedDecoratorTransient<TService>(
        object? key,
        Func<IServiceProvider, object?, TService> implementationFactory)
        where TService : class =>
        _services.AddKeyedDecoratorTransient(key, implementationFactory);

    /// <summary>
    /// Builds a provider from the configured services.
    /// </summary>
    /// <returns>Built <see cref="ServiceProvider"/>.</returns>
    public IServiceProvider UseGenDI() =>
        _services.UseGenDI();

    private static void ThrowIfNull(object? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}

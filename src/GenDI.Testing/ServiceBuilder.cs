using Microsoft.Extensions.DependencyInjection;

namespace GenDI.Testing;

/// <summary>
/// Provides a fluent service-collection builder for unit and integration tests.
/// </summary>
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
    /// Builds a provider from the configured services.
    /// </summary>
    /// <param name="validateScopes">Whether to validate scopes on build.</param>
    /// <param name="validateOnBuild">Whether to validate service graph on build.</param>
    /// <returns>Built <see cref="ServiceProvider"/>.</returns>
    public ServiceProvider BuildServiceProvider(
        bool validateScopes = true,
        bool validateOnBuild = true
    ) =>
        _services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateScopes = validateScopes,
                ValidateOnBuild = validateOnBuild,
            }
        );

    private static void ThrowIfNull(object? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}

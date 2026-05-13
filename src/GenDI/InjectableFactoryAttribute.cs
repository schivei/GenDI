using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Marks a static factory method for source-generated registration.
/// </summary>
/// <remarks>
/// Open-generic service shapes are not supported by generated registration and are ignored by the source generator.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class InjectableFactoryAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectableFactoryAttribute"/> class.
    /// </summary>
    public InjectableFactoryAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectableFactoryAttribute"/> class.
    /// </summary>
#pragma warning disable S1133 // kept intentionally for compatibility while guiding migration to the generic attribute
    [Obsolete(
        "Use InjectableFactoryAttribute<TService> instead of typeof-based service selection.",
        error: false
    )]
#pragma warning restore S1133
    public InjectableFactoryAttribute(Type serviceType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }

    /// <summary>
    /// Gets the lifetime used by generated registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets the explicit service contract for the factory registration.
    /// </summary>
    public Type? ServiceType { get; }

    /// <summary>
    /// Optional order value inside a group.
    /// </summary>
    public int Order { get; set; } = InjectableAttribute.DefaultOrderingValue;

    /// <summary>
    /// Optional group value used as first ordering key.
    /// </summary>
    public int Group { get; set; } = InjectableAttribute.DefaultOrderingValue;

    /// <summary>
    /// Optional keyed-service identifier for keyed factory registration.
    /// </summary>
    public object? Key { get; set; }

    /// <summary>
    /// Optional thread-isolation registration policy.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;

    /// <summary>
    /// Optional registration module name used for grouped registration.
    /// </summary>
    public string? Module { get; set; }
}

/// <summary>
/// Marks a static factory method for source-generated registration with an explicit service contract.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class InjectableFactoryAttribute<TService> : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectableFactoryAttribute{TService}"/> class.
    /// </summary>
    public InjectableFactoryAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Gets the explicit service contract for the factory registration.
    /// </summary>
    public Type ServiceType => typeof(TService);

    /// <summary>
    /// Gets the lifetime used by generated registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Optional order value inside a group.
    /// </summary>
    public int Order { get; set; } = InjectableAttribute.DefaultOrderingValue;

    /// <summary>
    /// Optional group value used as first ordering key.
    /// </summary>
    public int Group { get; set; } = InjectableAttribute.DefaultOrderingValue;

    /// <summary>
    /// Optional keyed-service identifier for keyed factory registration.
    /// </summary>
    public object? Key { get; set; }

    /// <summary>
    /// Optional thread-isolation registration policy.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;

    /// <summary>
    /// Optional registration module name used for grouped registration.
    /// </summary>
    public string? Module { get; set; }
}

using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Marks a static factory method for source-generated registration.
/// </summary>
/// <remarks>
/// Open-generic service shapes are not supported by generated registration and are ignored by the source generator.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="InjectableFactoryAttribute"/> class.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class InjectableFactoryAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient) : Attribute
{

    /// <summary>
    /// Gets the lifetime used by generated registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// Gets the explicit service contract for the factory registration.
    /// </summary>
    public Type? ServiceType { get; } = null;

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
    public object? Key { get; set; } = null;

    /// <summary>
    /// Optional thread-isolation registration policy.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;

    /// <summary>
    /// Optional registration module name used for grouped registration.
    /// </summary>
    public string? Module { get; set; } = null;
}

/// <summary>
/// Marks a static factory method for source-generated registration with an explicit service contract.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InjectableFactoryAttribute{TService}"/> class.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class InjectableFactoryAttribute<TService>(ServiceLifetime lifetime = ServiceLifetime.Transient) : Attribute
{

    /// <summary>
    /// Gets the explicit service contract for the factory registration.
    /// </summary>
    public Type ServiceType => typeof(TService);

    /// <summary>
    /// Gets the lifetime used by generated registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

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
    public object? Key { get; set; } = null;

    /// <summary>
    /// Optional thread-isolation registration policy.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;

    /// <summary>
    /// Optional registration module name used for grouped registration.
    /// </summary>
    public string? Module { get; set; } = null;
}

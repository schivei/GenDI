using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Marks a concrete class for source-generated dependency injection registration.
/// </summary>
/// <remarks>
/// Use this non-generic variant when no explicit service contract is required.
/// In this case, <see cref="ServiceType"/> always returns <see langword="null"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableAttribute : Attribute
{
    /// <summary>
    /// Default value for ordering members when no explicit value is provided.
    /// Registrations with this value are emitted after lower values and use service type name (ordinal) as a tie-breaker.
    /// </summary>
    public const int DefaultOrderingValue = int.MaxValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectableAttribute"/> class.
    /// </summary>
    /// <param name="lifetime">The service lifetime for the generated registration.</param>
    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Gets the lifetime used by the generated registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Explicit service contract for non-generic usage. Always <see langword="null"/>.
    /// Use <see cref="InjectableAttribute{TService}"/> to define an explicit contract safely.
    /// </summary>
    #pragma warning disable CA1822 // kept as instance member for API parity with generic variant
    #pragma warning disable S2325 // kept as instance member for API parity with generic variant
    public Type? ServiceType => null;
    #pragma warning restore S2325
    #pragma warning restore CA1822

    /// <summary>
    /// Optional order value inside a group. Defaults to <see cref="DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Order { get; set; } = DefaultOrderingValue;

    /// <summary>
    /// Optional group value used as first ordering key. Defaults to <see cref="DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Group { get; set; } = DefaultOrderingValue;

    /// <summary>
    /// Optional keyed-service identifier used for generated keyed registrations.
    /// Defaults to <see langword="null"/> (non-keyed registration).
    /// </summary>
    public object? Key { get; set; }

    /// <summary>
    /// Optional thread-isolation registration lifetime override.
    /// When set, generated registration resolves through a thread-local cache.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;

    /// <summary>
    /// Optional registration module name used for grouped registration.
    /// </summary>
    public string? Module { get; set; }
}

/// <summary>
/// Marks a concrete class for source-generated registration with an explicit service contract.
/// </summary>
/// <typeparam name="TService">The service contract type to register.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableAttribute<TService> : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectableAttribute{TService}"/> class.
    /// </summary>
    /// <param name="lifetime">The service lifetime for the generated registration.</param>
    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Gets the lifetime used by the generated registration.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Explicit service contract inferred from <typeparamref name="TService"/>.
    /// </summary>
    #pragma warning disable S2325 // kept as instance member for API parity with non-generic variant
    public Type ServiceType => typeof(TService);
    #pragma warning restore S2325

    /// <summary>
    /// Optional order value inside a group. Defaults to <see cref="InjectableAttribute.DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Order { get; set; } = InjectableAttribute.DefaultOrderingValue;

    /// <summary>
    /// Optional group value used as first ordering key. Defaults to <see cref="InjectableAttribute.DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Group { get; set; } = InjectableAttribute.DefaultOrderingValue;

    /// <summary>
    /// Optional keyed-service identifier used for generated keyed registrations.
    /// Defaults to <see langword="null"/> (non-keyed registration).
    /// </summary>
    public object? Key { get; set; }

    /// <summary>
    /// Optional thread-isolation registration lifetime override.
    /// When set, generated registration resolves through a thread-local cache.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;

    /// <summary>
    /// Optional registration module name used for grouped registration.
    /// </summary>
    public string? Module { get; set; }
}

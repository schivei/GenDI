using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableAttribute : Attribute
{
    /// <summary>
    /// Default value for ordering members when no explicit value is provided.
    /// Registrations with this value are emitted after lower values and use service type name (ordinal) as a tie-breaker.
    /// </summary>
    public const int DefaultOrderingValue = int.MaxValue;

    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Explicit service contract for non-generic usage. Always <see langword="null"/>.
    /// Use <see cref="InjectableAttribute{TService}"/> to define an explicit contract safely.
    /// </summary>
    public Type? ServiceType => null;

    /// <summary>
    /// Optional order value inside a group. Defaults to <see cref="DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Order { get; set; } = DefaultOrderingValue;

    /// <summary>
    /// Optional group value used as first ordering key. Defaults to <see cref="DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Group { get; set; } = DefaultOrderingValue;
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableAttribute<TService> : Attribute
{
    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Explicit service contract inferred from <typeparamref name="TService"/>.
    /// </summary>
    public Type ServiceType => typeof(TService);

    /// <summary>
    /// Optional order value inside a group. Defaults to <see cref="InjectableAttribute.DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Order { get; set; } = InjectableAttribute.DefaultOrderingValue;

    /// <summary>
    /// Optional group value used as first ordering key. Defaults to <see cref="InjectableAttribute.DefaultOrderingValue"/> (<see cref="int.MaxValue"/>).
    /// </summary>
    public int Group { get; set; } = InjectableAttribute.DefaultOrderingValue;
}

using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableAttribute : Attribute
{
    /// <summary>
    /// Default value for <see cref="Order"/> and <see cref="Group"/> when no explicit ordering is provided.
    /// Registrations with this value are emitted after lower values.
    /// </summary>
    public const int DefaultOrderGroup = int.MaxValue;

    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
        Order = DefaultOrderGroup;
        Group = DefaultOrderGroup;
    }

    public ServiceLifetime Lifetime { get; }

    public Type? ServiceType { get; set; }

    public int Order { get; set; }

    public int Group { get; set; }
}

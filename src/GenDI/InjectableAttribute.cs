using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableAttribute : Attribute
{
    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
        Order = int.MaxValue;
        Group = int.MaxValue;
    }

    public ServiceLifetime Lifetime { get; }

    public Type? ServiceType { get; set; }

    public int Order { get; set; }

    public int Group { get; set; }
}

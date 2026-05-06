using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableAttribute : Attribute
{
    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    public ServiceLifetime Lifetime { get; }

    public Type? ServiceType { get; init; }
}

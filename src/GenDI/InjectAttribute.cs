using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Marks an init-only property as eligible for generator-emitted property injection.
/// </summary>
/// <remarks>
/// Supported properties must be public or internal and declared as <c>get; init;</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class InjectAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectAttribute"/> class.
    /// </summary>
    public InjectAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectAttribute"/> class with a lifetime override.
    /// </summary>
    /// <param name="lifetime">Lifetime override used during indirect registration discovery.</param>
    public InjectAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Optional keyed-service identifier used when resolving this property.
    /// Defaults to <see langword="null"/> (non-keyed resolution).
    /// </summary>
    public object? Key { get; set; }

    /// <summary>
    /// Lifetime override used by indirect registration discovery.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = ServiceLifetime.Transient;
}

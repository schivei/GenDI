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
    /// Optional keyed-service identifier used when resolving this property.
    /// Defaults to <see langword="null"/> (non-keyed resolution).
    /// </summary>
    public object? Key { get; set; }
}

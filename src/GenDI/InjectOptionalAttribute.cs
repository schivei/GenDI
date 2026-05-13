namespace GenDI;

/// <summary>
/// Marks an init-only property for optional generator-emitted property injection.
/// </summary>
/// <remarks>
/// Optional injection resolves dependencies with <c>GetService</c>/<c>GetKeyedService</c>
/// and leaves the property unset (<see langword="null"/>) when no registration exists.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class InjectOptionalAttribute : Attribute
{
    /// <summary>
    /// Optional keyed-service identifier used when resolving this property.
    /// Defaults to <see langword="null"/> (non-keyed resolution).
    /// </summary>
    public object? Key { get; set; }
}

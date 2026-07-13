namespace GenDI;

/// <summary>
/// Marks an options type for automatic IOptions registration and optional configuration section selection.
/// </summary>
/// <param name="key">
/// Optional configuration key/path used to select the section.
/// When omitted, the options type name is used as the section key.
/// </param>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class OptionConfigAttribute(string? key = null) : Attribute
{
    /// <summary>
    /// Gets the optional configuration key/path.
    /// </summary>
    public string? Key { get; } = key;

    /// <summary>
    /// Gets the optional configuration key/path.
    /// </summary>
    public string? Path => Key;
}

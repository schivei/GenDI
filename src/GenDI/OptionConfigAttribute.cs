namespace GenDI;

/// <summary>
/// Marks an options type with a required configuration key/path for automatic IOptions registration.
/// </summary>
/// <param name="path">Configuration key/path used to bind the options instance.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class OptionConfigAttribute(string path) : Attribute
{
    /// <summary>
    /// Gets the required configuration key/path.
    /// </summary>
    public string Path { get; } = path;
}

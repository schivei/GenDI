namespace GenDI;

/// <summary>
/// Assigns a service/module grouping label to a class for grouped generated registration.
/// </summary>
/// <param name="name">Module name.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InjectableModuleAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the module name.
    /// </summary>
    public string Name { get; } = name;
}

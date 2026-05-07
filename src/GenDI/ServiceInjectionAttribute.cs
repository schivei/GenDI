namespace GenDI;

/// <summary>
/// Marks interfaces and abstract contracts that should be considered service contracts during generation.
/// </summary>
/// <remarks>
/// When an <c>[Injectable]</c> class implements or inherits contracts annotated with this attribute,
/// GenDI registers the generated activation for each discovered contract type.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Class,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class ServiceInjectionAttribute : Attribute { }

namespace GenDI;

/// <summary>
/// Applies an environment-based condition to source-generated registration.
/// </summary>
/// <remarks>
/// This attribute must be combined with <c>[Injectable]</c> or <c>[Injectable&lt;TService&gt;]</c>.
/// The generated registration executes only when <c>DOTNET_ENVIRONMENT</c> or
/// <c>ASPNETCORE_ENVIRONMENT</c> matches <see cref="EnvironmentName"/>.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ConditionalInjectableAttribute"/> class.
/// </remarks>
/// <param name="environmentName">Target environment name for conditional registration.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ConditionalInjectableAttribute(string environmentName) : Attribute
{
    /// <summary>
    /// Gets the target environment name for conditional registration.
    /// </summary>
    public string EnvironmentName { get; } = environmentName;

    /// <summary>
    /// Gets a value indicating whether the condition is negated. If <c>true</c>, the generated registration executes only when the environment does NOT match <see cref="EnvironmentName"/>.
    /// </summary>
    public bool Not { get; init; } = false;
}

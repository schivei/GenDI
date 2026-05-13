namespace GenDI;

/// <summary>
/// Applies an environment-based condition to source-generated registration.
/// </summary>
/// <remarks>
/// This attribute must be combined with <c>[Injectable]</c> or <c>[Injectable&lt;TService&gt;]</c>.
/// The generated registration executes only when <c>DOTNET_ENVIRONMENT</c> or
/// <c>ASPNETCORE_ENVIRONMENT</c> matches <see cref="EnvironmentName"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ConditionalInjectableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalInjectableAttribute"/> class.
    /// </summary>
    /// <param name="environmentName">Target environment name for conditional registration.</param>
    public ConditionalInjectableAttribute(string environmentName)
    {
        EnvironmentName = environmentName;
    }

    /// <summary>
    /// Gets the target environment name for conditional registration.
    /// </summary>
    public string EnvironmentName { get; }
}

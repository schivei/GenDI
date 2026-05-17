namespace GenDI;

/// <summary>
/// Controls whether the generated <c>AddGenDIServices()</c> extension remains included in code coverage metrics.
/// Apply this attribute at assembly level in the consuming project.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class GenDiCoverationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenDiCoverationAttribute"/> class.
    /// </summary>
    /// <param name="includeGeneratedCodeInCoverage">
    /// <see langword="true"/> to keep generated extension code included in coverage (default);
    /// <see langword="false"/> to mark generated extension code with <c>[ExcludeFromCodeCoverage]</c>.
    /// </param>
    public GenDiCoverationAttribute(bool includeGeneratedCodeInCoverage = true)
    {
        IncludeGeneratedCodeInCoverage = includeGeneratedCodeInCoverage;
    }

    /// <summary>
    /// Gets a value indicating whether generated extension code should be included in code coverage metrics.
    /// </summary>
    public bool IncludeGeneratedCodeInCoverage { get; }
}

namespace GenDI;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class GenDICoverationAttribute : Attribute
{
    public GenDICoverationAttribute(bool includeGeneratedCodeInCoverage = true)
    {
        IncludeGeneratedCodeInCoverage = includeGeneratedCodeInCoverage;
    }

    public bool IncludeGeneratedCodeInCoverage { get; }
}

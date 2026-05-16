using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

internal sealed class OpenGenericBypassWarning
{
    public OpenGenericBypassWarning(Location location, string context, string typeDisplay)
    {
        Location = location;
        Context = context;
        TypeDisplay = typeDisplay;
    }

    public Location Location { get; }

    public string Context { get; }

    public string TypeDisplay { get; }
}

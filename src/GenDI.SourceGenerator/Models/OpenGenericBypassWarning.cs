using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

internal sealed class OpenGenericBypassWarning(Location location, string context, string typeDisplay)
{
    public Location Location { get; } = location;

    public string Context { get; } = context;

    public string TypeDisplay { get; } = typeDisplay;
}

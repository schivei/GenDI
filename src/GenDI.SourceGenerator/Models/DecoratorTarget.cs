using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

internal sealed class DecoratorTarget(INamedTypeSymbol serviceType, string displayName, int order)
{
    public INamedTypeSymbol ServiceType { get; } = serviceType;

    public string DisplayName { get; } = displayName;

    public int Order { get; } = order;
}

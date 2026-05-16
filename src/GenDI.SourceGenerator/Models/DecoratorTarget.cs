using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

internal sealed class DecoratorTarget
{
    public DecoratorTarget(INamedTypeSymbol serviceType, string displayName, int order)
    {
        ServiceType = serviceType;
        DisplayName = displayName;
        Order = order;
    }

    public INamedTypeSymbol ServiceType { get; }

    public string DisplayName { get; }

    public int Order { get; }
}

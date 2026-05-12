using Microsoft.CodeAnalysis;

namespace GenDI.Analyzers;

internal static class GenDIDiagnostics
{
    public static readonly DiagnosticDescriptor InjectRequiresInitOnlyProperty = new(
        id: "GENDI001",
        title: "Inject attribute requires init-only property",
        messageFormat: "Property '{0}' uses [Inject] and must declare an init-only setter",
        category: "GenDI.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GenDI property injection supports only init-only properties."
    );

    public static readonly DiagnosticDescriptor InjectableRequiresConcreteClass = new(
        id: "GENDI002",
        title: "Injectable attribute requires concrete class",
        messageFormat: "Type '{0}' uses [Injectable] and must be a non-abstract class",
        category: "GenDI.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GenDI registers only concrete implementation types."
    );
}

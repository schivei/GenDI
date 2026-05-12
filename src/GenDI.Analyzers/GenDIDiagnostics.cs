using Microsoft.CodeAnalysis;

namespace GenDI.Analyzers;

internal static class GenDIDiagnostics
{
    private const string AnalyzerDiagnosticsDocBaseUrl =
        "https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md";

    public static readonly DiagnosticDescriptor InjectRequiresInitOnlyProperty = new(
        id: "GENDI001",
        title: "Inject attribute requires init-only property",
        messageFormat: "Property '{0}' uses [Inject] and must declare an init-only setter",
        category: "GenDI.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GenDI property injection supports only init-only properties.",
        helpLinkUri: $"{AnalyzerDiagnosticsDocBaseUrl}#gendi001---inject-attribute-requires-init-only-property"
    );

    public static readonly DiagnosticDescriptor InjectableRequiresConcreteClass = new(
        id: "GENDI002",
        title: "Injectable attribute requires concrete class",
        messageFormat: "Type '{0}' uses [Injectable] and must be a non-abstract class",
        category: "GenDI.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GenDI registers only concrete implementation types.",
        helpLinkUri: $"{AnalyzerDiagnosticsDocBaseUrl}#gendi002---injectable-attribute-requires-concrete-class"
    );

    public static readonly DiagnosticDescriptor ConstructorInjectionCanBeConverted = new(
        id: "GENDI003",
        title: "Constructor injection can be converted to GenDI property injection",
        messageFormat:
            "Constructor injection in '{0}' can be converted to [Inject] init-only properties",
        category: "GenDI.Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description:
            "Use a code fix to migrate constructor-injected dependencies to GenDI property injection.",
        helpLinkUri:
            $"{AnalyzerDiagnosticsDocBaseUrl}#gendi003---constructor-injection-can-be-converted-to-gendi-property-injection"
    );
}

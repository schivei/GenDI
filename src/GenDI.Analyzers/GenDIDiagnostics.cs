using Microsoft.CodeAnalysis;

namespace GenDI.Analyzers;

internal static class GenDiDiagnostics
{
#pragma warning disable S1075 // external diagnostics documentation URL is intentionally fixed
    private const string AnalyzerDiagnosticsDocBaseUrl =
        "https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md";
#pragma warning restore S1075
    private const string UsageCategory = "GenDI.Usage";

    public static readonly DiagnosticDescriptor InjectRequiresInitOnlyProperty = new(
        id: "GENDI001",
        title: "Inject attribute requires init-only property",
        messageFormat: "Property '{0}' uses [Inject] and must declare an init-only setter",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GenDI property injection supports only init-only properties.",
        helpLinkUri: $"{AnalyzerDiagnosticsDocBaseUrl}#gendi001---inject-attribute-requires-init-only-property"
    );

    public static readonly DiagnosticDescriptor InjectableRequiresConcreteClass = new(
        id: "GENDI002",
        title: "Injectable attribute requires concrete class",
        messageFormat: "Type '{0}' uses [Injectable] and must be a non-abstract class",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "GenDI registers only concrete implementation types.",
        helpLinkUri: $"{AnalyzerDiagnosticsDocBaseUrl}#gendi002---injectable-attribute-requires-concrete-class"
    );

    public static readonly DiagnosticDescriptor ConstructorInjectionCanBeConverted = new(
        id: "GENDI003",
        title: "Constructor injection can be converted to GenDI property injection",
        messageFormat: "Constructor injection in '{0}' can be converted to [Inject] init-only properties",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Use a code fix to migrate constructor-injected dependencies to GenDI property injection.",
        helpLinkUri: $"{AnalyzerDiagnosticsDocBaseUrl}#gendi003---constructor-injection-can-be-converted-to-gendi-property-injection"
    );

    public static readonly DiagnosticDescriptor DecoratorRequiresResolvableContract = new(
        id: "GENDI004",
        title: "Decorator attribute requires a resolvable service contract",
        messageFormat: "Decorator '{0}' must declare or infer exactly one closed [ServiceInjection] contract",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Non-generic [DecoratorFor] usage requires exactly one closed [ServiceInjection] contract in the decorator inheritance or implementation chain.",
        helpLinkUri: $"{AnalyzerDiagnosticsDocBaseUrl}#gendi004---decorator-attribute-requires-a-resolvable-service-contract"
    );

    public static readonly DiagnosticDescriptor DecoratorRequiresInnerDependency = new(
        id: "GENDI005",
        title: "Decorator requires an inner dependency",
        messageFormat: "Decorator '{0}' must declare a public constructor parameter or [Inject] init-only property of type '{1}'",
        category: UsageCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Decorators must expose an injectable dependency matching the decorated service contract so the generated pipeline can be composed statically.",
        helpLinkUri: $"{AnalyzerDiagnosticsDocBaseUrl}#gendi005---decorator-requires-an-inner-dependency"
    );
}

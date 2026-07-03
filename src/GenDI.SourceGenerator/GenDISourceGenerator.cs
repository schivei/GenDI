using System.Collections.Immutable;
using GenDI.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenDI.SourceGenerator;

[Generator]
public sealed partial class GenDISourceGenerator : IIncrementalGenerator
{
    private const int DefaultOrderingValue = int.MaxValue;
    private static readonly DiagnosticDescriptor OpenGenericBypassWarningDescriptor = new(
        id: "GENDISG001",
        title: "Open-generic type ignored by GenDI source generation",
        messageFormat: "GenDI ignored open-generic type '{0}' in {1}. Only closed-generic types are supported for generated registrations.",
        category: "GenDI.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classSymbols = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => IsCandidateClassDeclaration(node),
                static (generatorContext, _) =>
                    generatorContext.SemanticModel.GetDeclaredSymbol(
                        (ClassDeclarationSyntax)generatorContext.Node
                    ) as INamedTypeSymbol
            )
            .Where(static symbol => symbol is not null)
            .Collect();

        var generationOptions = context.CompilationProvider.Select(
            static (compilation, _) =>
                (
                    Compilation: compilation,
                    Namespace: GetProjectNamespace(compilation),
                    IncludeExcludeFromCodeCoverage: !IsGeneratedCodeCoverageEnabled(compilation)
                )
        );

        var generationInput = classSymbols.Combine(generationOptions);

        context.RegisterSourceOutput(
            generationInput,
            static (sourceProductionContext, source) =>
            {
                var (discoveredTypes, options) = source;
                var sourceTypes = discoveredTypes
                    .Where(static symbol => symbol is not null)
                    .Cast<ISymbol>()
                    .Distinct(SymbolEqualityComparer.Default)
                    .Cast<INamedTypeSymbol>()
                    .ToImmutableArray();
                var buildResult = BuildRegistrations(options.Compilation, sourceTypes);
                foreach (var warning in buildResult.Warnings)
                {
                    sourceProductionContext.ReportDiagnostic(
                        Diagnostic.Create(
                            OpenGenericBypassWarningDescriptor,
                            warning.Location,
                            warning.TypeDisplay,
                            warning.Context
                        )
                    );
                }

                var normalizedRegistrations = buildResult.Registrations
                    .Distinct(ServiceRegistrationComparer.Instance)
                    .OrderBy(static registration => registration.IsDecorator)
                    .ThenBy(static registration => registration.Group)
                    .ThenBy(static registration => registration.Order)
                    .ThenBy(static registration => registration.ServiceType, StringComparer.Ordinal)
                    .ToImmutableArray();

                sourceProductionContext.AddSource(
                    "GenDIServiceCollectionExtensions.g.cs",
                    BuildGeneratedSource(
                        normalizedRegistrations,
                        options.Namespace,
                        includeExcludeFromCodeCoverage: options.IncludeExcludeFromCodeCoverage
                    )
                );
            }
        );
    }

    private static bool IsCandidateClassDeclaration(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
        {
            return false;
        }

        if (HasCandidateAttributeName(classDeclaration.AttributeLists))
        {
            return true;
        }

        if (classDeclaration.BaseList is not null)
        {
            return true;
        }

        foreach (var member in classDeclaration.Members)
        {
            if (
                member is MethodDeclarationSyntax method
                && HasCandidateAttributeName(method.AttributeLists)
            )
            {
                return true;
            }

            if (
                member is PropertyDeclarationSyntax property
                && HasCandidateAttributeName(property.AttributeLists)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCandidateAttributeName(SyntaxList<AttributeListSyntax> attributeLists)
    {
        if (attributeLists.Count == 0)
        {
            return false;
        }

        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (IsCandidateAttributeName(attributeName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsCandidateAttributeName(string attributeName)
    {
        return attributeName.Contains("Injectable", StringComparison.Ordinal)
            || attributeName.Contains("ServiceInjection", StringComparison.Ordinal)
            || attributeName.Contains("DecoratorFor", StringComparison.Ordinal)
            || attributeName.Contains("Inject", StringComparison.Ordinal)
            || attributeName.Contains("OptionConfig", StringComparison.Ordinal)
            || attributeName.Contains("ConditionalInjectable", StringComparison.Ordinal)
            || attributeName.Contains("InjectableModule", StringComparison.Ordinal)
            || attributeName.Contains("InjectableFactory", StringComparison.Ordinal)
            || attributeName.Contains("GenDICoveration", StringComparison.Ordinal);
    }

    private static bool IsInjectableAttribute(INamedTypeSymbol attributeClass)
    {
        var definitionDisplay = attributeClass.OriginalDefinition.ToDisplayString();
        return definitionDisplay
            is "GenDI.InjectableAttribute"
                or "GenDI.InjectableAttribute<TService>";
    }
}

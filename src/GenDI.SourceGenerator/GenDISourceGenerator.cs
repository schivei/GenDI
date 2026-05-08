using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenDI.SourceGenerator;

[Generator]
public sealed partial class GenDISourceGenerator : IIncrementalGenerator
{
    private const int DefaultOrderingValue = int.MaxValue;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (node, _) =>
                    node is ClassDeclarationSyntax classDeclaration
                    && HasInjectableAttributeSyntax(classDeclaration),
                static (generatorContext, _) =>
                    generatorContext.SemanticModel.GetDeclaredSymbol(
                        (ClassDeclarationSyntax)generatorContext.Node
                    ) as INamedTypeSymbol
            )
            .Where(static symbol => symbol is not null)
            .Select(static (symbol, _) => symbol!)
            .Collect();

        var generationInput = context.CompilationProvider.Combine(candidates);

        context.RegisterSourceOutput(
            generationInput,
            static (sourceProductionContext, source) =>
            {
                var (compilation, symbols) = source;
                var registrations = symbols
                    .SelectMany(BuildRegistrations)
                    .Distinct(ServiceRegistrationComparer.Instance)
                    .OrderBy(static registration => registration.Group)
                    .ThenBy(static registration => registration.Order)
                    .ThenBy(static registration => registration.ServiceType, StringComparer.Ordinal)
                    .ToImmutableArray();

                if (registrations.Length == 0)
                {
                    return;
                }

                sourceProductionContext.AddSource(
                    "GenDIServiceCollectionExtensions.g.cs",
                    BuildGeneratedSource(
                        registrations,
                        GetProjectNamespace(compilation),
                        includeExcludeFromCodeCoverage: !IsGeneratedCodeCoverageEnabled(compilation)
                    )
                );
            }
        );
    }

    private static bool HasInjectableAttributeSyntax(ClassDeclarationSyntax classDeclaration)
    {
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                var normalizedName = attributeName.StartsWith("global::", StringComparison.Ordinal)
                    ? attributeName.Substring("global::".Length)
                    : attributeName;
                if (
                    normalizedName
                        is "Injectable"
                            or "InjectableAttribute"
                            or "GenDI.Injectable"
                            or "GenDI.InjectableAttribute"
                    || normalizedName.StartsWith("Injectable<", StringComparison.Ordinal)
                    || normalizedName.StartsWith("InjectableAttribute<", StringComparison.Ordinal)
                    || normalizedName.StartsWith("GenDI.Injectable<", StringComparison.Ordinal)
                    || normalizedName.StartsWith(
                        "GenDI.InjectableAttribute<",
                        StringComparison.Ordinal
                    )
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsInjectableAttribute(INamedTypeSymbol attributeClass)
    {
        var definitionDisplay = attributeClass.OriginalDefinition.ToDisplayString();
        return definitionDisplay
            is "GenDI.InjectableAttribute"
                or "GenDI.InjectableAttribute<TService>";
    }
}

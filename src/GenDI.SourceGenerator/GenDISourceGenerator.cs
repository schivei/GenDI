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
        var classSymbols = context
            .SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
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
                var allTypes = discoveredTypes
                    .Where(static symbol => symbol is not null)
                    .Cast<ISymbol>()
                    .Concat(GetReferencedAssemblyTypes(options.Compilation).Cast<ISymbol>())
                    .Distinct(SymbolEqualityComparer.Default)
                    .Cast<INamedTypeSymbol>()
                    .ToImmutableArray();
                var registrationCandidates = BuildRegistrations(allTypes);
                var normalizedRegistrations = registrationCandidates
                    .Distinct(ServiceRegistrationComparer.Instance)
                    .OrderBy(static registration => registration.Group)
                    .ThenBy(static registration => registration.Order)
                    .ThenBy(static registration => registration.ServiceType, StringComparer.Ordinal)
                    .ToImmutableArray();

                if (normalizedRegistrations.Length == 0)
                {
                    return;
                }

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

    private static bool IsInjectableAttribute(INamedTypeSymbol attributeClass)
    {
        var definitionDisplay = attributeClass.OriginalDefinition.ToDisplayString();
        return definitionDisplay
            is "GenDI.InjectableAttribute"
                or "GenDI.InjectableAttribute<TService>";
    }
}

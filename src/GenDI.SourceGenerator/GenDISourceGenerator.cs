using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenDI.SourceGenerator;

[Generator]
public sealed partial class GenDISourceGenerator : ISourceGenerator
{
    private const int DefaultOrderingValue = int.MaxValue;

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var symbols = context
            .Compilation.SyntaxTrees.SelectMany(static syntaxTree => syntaxTree.GetRoot().DescendantNodes())
            .OfType<ClassDeclarationSyntax>()
            .Where(HasInjectableAttributeSyntax)
            .Select(classDeclaration =>
                context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree).GetDeclaredSymbol(classDeclaration)
                    as INamedTypeSymbol
            )
            .Where(static symbol => symbol is not null)
            .Select(static symbol => symbol!)
            .ToImmutableArray();

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

        context.AddSource(
            "GenDIServiceCollectionExtensions.g.cs",
            BuildGeneratedSource(
                registrations,
                GetProjectNamespace(context.Compilation),
                includeExcludeFromCodeCoverage: !IsGeneratedCodeCoverageEnabled(context.Compilation)
            )
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

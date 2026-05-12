using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GenDI.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InjectableUsageAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedRules =
    [
        GenDIDiagnostics.InjectRequiresInitOnlyProperty,
        GenDIDiagnostics.InjectableRequiresConcreteClass,
        GenDIDiagnostics.ConstructorInjectionCanBeConverted,
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedRules;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(
            AnalyzeConstructorDeclaration,
            Microsoft.CodeAnalysis.CSharp.SyntaxKind.ConstructorDeclaration
        );
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol property || !HasInjectAttribute(property))
        {
            return;
        }

        if (property.SetMethod is { IsInitOnly: true })
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                GenDIDiagnostics.InjectRequiresInitOnlyProperty,
                property.Locations.FirstOrDefault(),
                property.Name
            )
        );
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (
            context.Symbol is not INamedTypeSymbol typeSymbol
            || !HasInjectableAttribute(typeSymbol)
        )
        {
            return;
        }

        if (typeSymbol.TypeKind == TypeKind.Class && !typeSymbol.IsAbstract)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                GenDIDiagnostics.InjectableRequiresConcreteClass,
                typeSymbol.Locations.FirstOrDefault(),
                typeSymbol.Name
            )
        );
    }

    private static bool HasInjectAttribute(IPropertySymbol propertySymbol)
    {
        return propertySymbol
            .GetAttributes()
            .Any(attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == "GenDI.InjectAttribute"
            );
    }

    private static bool HasInjectableAttribute(INamedTypeSymbol typeSymbol)
    {
        foreach (var attributeData in typeSymbol.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            var definitionName = attributeClass.OriginalDefinition.ToDisplayString();
            if (
                definitionName == "GenDI.InjectableAttribute"
                || definitionName == "GenDI.InjectableAttribute<TService>"
            )
            {
                return true;
            }
        }

        return false;
    }

    private static void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ConstructorDeclarationSyntax constructorDeclaration)
        {
            return;
        }

        if (
            constructorDeclaration.ParameterList.Parameters.Count == 0
            || constructorDeclaration.Body is null
            || constructorDeclaration.Body.Statements.Count > 0
            || constructorDeclaration.ExpressionBody is not null
        )
        {
            return;
        }

        if (
            context.SemanticModel.GetDeclaredSymbol(
                constructorDeclaration,
                context.CancellationToken
            )
                is not IMethodSymbol constructorSymbol
            || constructorSymbol.ContainingType is null
            || !HasInjectableAttribute(constructorSymbol.ContainingType)
            || constructorSymbol.DeclaredAccessibility != Accessibility.Public
        )
        {
            return;
        }

        var propagatedParameterNames = GetPropagatedParameterNames(constructorDeclaration);
        var hasConvertibleParameter = constructorDeclaration.ParameterList.Parameters.Any(
            parameter => !propagatedParameterNames.Contains(parameter.Identifier.ValueText)
        );
        if (!hasConvertibleParameter)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                GenDIDiagnostics.ConstructorInjectionCanBeConverted,
                constructorDeclaration.GetLocation(),
                constructorSymbol.ContainingType.Name
            )
        );
    }

    private static HashSet<string> GetPropagatedParameterNames(
        ConstructorDeclarationSyntax constructorDeclaration
    )
    {
        if (constructorDeclaration.Initializer?.ArgumentList is null)
        {
            return [];
        }

        var propagatedParameterNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var argument in constructorDeclaration.Initializer.ArgumentList.Arguments)
        {
            if (argument.Expression is IdentifierNameSyntax identifierName)
            {
                propagatedParameterNames.Add(identifierName.Identifier.ValueText);
            }
        }

        return propagatedParameterNames;
    }
}

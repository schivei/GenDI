using System.Collections.Immutable;
using System.Linq;
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
        GenDIDiagnostics.DecoratorRequiresResolvableContract,
        GenDIDiagnostics.DecoratorRequiresInnerDependency,
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

        if (
            property.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal)
            || property.SetMethod is null
            || property.SetMethod.DeclaredAccessibility
                is not (Accessibility.Public or Accessibility.Internal)
        )
        {
            return;
        }

        if (property.SetMethod.IsInitOnly)
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
        if (context.Symbol is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        if (HasInjectableAttribute(typeSymbol) && !(typeSymbol.TypeKind == TypeKind.Class && !typeSymbol.IsAbstract))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    GenDIDiagnostics.InjectableRequiresConcreteClass,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.Name
                )
            );
        }

        AnalyzeDecoratorType(context, typeSymbol);
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
        return typeSymbol
            .GetAttributes()
            .Select(static attributeData => attributeData.AttributeClass)
            .OfType<INamedTypeSymbol>()
            .Any(attributeClass =>
            {
                var definitionName = attributeClass.OriginalDefinition.ToDisplayString();
                return definitionName is "GenDI.InjectableAttribute" or "GenDI.InjectableAttribute<TService>";
            });
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
            || ConstructorInjectionAnalysisHelpers.HasMeaningfulBodyTrivia(
                constructorDeclaration.Body
            )
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

        var propagation = ConstructorInjectionAnalysisHelpers.TryGetPropagatedParameterNames(
            constructorDeclaration
        );
        if (!propagation.IsSafe)
        {
            return;
        }

        var hasConvertibleParameter = constructorDeclaration.ParameterList.Parameters.Any(
            parameter => !propagation.ParameterNames.Contains(parameter.Identifier.ValueText)
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

    private static void AnalyzeDecoratorType(
        SymbolAnalysisContext context,
        INamedTypeSymbol typeSymbol
    )
    {
        var hasDecoratorAttribute = false;
        var requiresInferredContract = false;
        var decoratedContracts = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        foreach (var attributeData in typeSymbol.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is null)
            {
                continue;
            }

            if (
                attributeClass.OriginalDefinition.ToDisplayString()
                == "GenDI.DecoratorForAttribute<TService>"
            )
            {
                hasDecoratorAttribute = true;
                if (
                    attributeClass.TypeArguments.Length == 1
                    && attributeClass.TypeArguments[0] is INamedTypeSymbol serviceType
                )
                {
                    decoratedContracts.Add(serviceType);
                }

                continue;
            }

            if (attributeClass.ToDisplayString() == "GenDI.DecoratorForAttribute")
            {
                hasDecoratorAttribute = true;
                requiresInferredContract = true;
            }
        }

        if (!hasDecoratorAttribute)
        {
            return;
        }

        if (requiresInferredContract)
        {
            var inferredContracts = GetClosedServiceInjectionContracts(typeSymbol);
            if (inferredContracts.Length != 1)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        GenDIDiagnostics.DecoratorRequiresResolvableContract,
                        typeSymbol.Locations.FirstOrDefault(),
                        typeSymbol.Name
                    )
                );
                return;
            }

            decoratedContracts.Add(inferredContracts[0]);
        }

        foreach (
            var decoratedContract in decoratedContracts
                .Distinct(SymbolEqualityComparer.Default)
                .OfType<INamedTypeSymbol>()
        )
        {
            if (HasResolvableInnerDependency(typeSymbol, decoratedContract))
            {
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    GenDIDiagnostics.DecoratorRequiresInnerDependency,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.Name,
                    decoratedContract.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
                )
            );
        }
    }

    private static ImmutableArray<INamedTypeSymbol> GetClosedServiceInjectionContracts(
        INamedTypeSymbol symbol
    )
    {
        var serviceContracts = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        foreach (var interfaceSymbol in symbol.AllInterfaces)
        {
            if (HasServiceInjectionAttribute(interfaceSymbol) && !IsOpenGeneric(interfaceSymbol))
            {
                serviceContracts.Add(interfaceSymbol);
            }
        }

        var baseType = symbol.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (HasServiceInjectionAttribute(baseType) && !IsOpenGeneric(baseType))
            {
                serviceContracts.Add(baseType);
            }

            baseType = baseType.BaseType;
        }

        return serviceContracts
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<INamedTypeSymbol>()
            .ToImmutableArray();
    }

    private static bool HasServiceInjectionAttribute(ITypeSymbol symbol)
    {
        return symbol
            .GetAttributes()
            .Any(attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == "GenDI.ServiceInjectionAttribute"
            );
    }

    private static bool HasResolvableInnerDependency(
        INamedTypeSymbol decoratorType,
        INamedTypeSymbol serviceType
    )
    {
        return decoratorType
                .InstanceConstructors.Where(static constructor =>
                    constructor.DeclaredAccessibility == Accessibility.Public
                )
                .SelectMany(static constructor => constructor.Parameters)
                .Any(parameter => SymbolEqualityComparer.Default.Equals(parameter.Type, serviceType))
            || decoratorType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Any(property =>
                    property.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
                    && property.SetMethod is not null
                    && property.SetMethod.IsInitOnly
                    && property.SetMethod.DeclaredAccessibility
                        is Accessibility.Public or Accessibility.Internal
                    && SymbolEqualityComparer.Default.Equals(property.Type, serviceType)
                    && HasInjectAttribute(property)
                );
    }

    private static bool IsOpenGeneric(INamedTypeSymbol symbol)
    {
        return symbol.IsUnboundGenericType
            || symbol.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter);
    }
}

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace GenDI.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstructorInjectionCodeFixProvider))]
[Shared]
public sealed class ConstructorInjectionCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [GenDIDiagnostics.ConstructorInjectionCanBeConverted.Id];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context
            .Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        if (
            root is null
            || root.FindNode(diagnostic.Location.SourceSpan)
                is not ConstructorDeclarationSyntax constructor
        )
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Convert constructor injection to [Inject] properties",
                createChangedDocument: cancellationToken =>
                    ConvertAsync(context.Document, constructor, cancellationToken),
                equivalenceKey: nameof(ConstructorInjectionCodeFixProvider)
            ),
            diagnostic
        );
    }

    private static async Task<Document> ConvertAsync(
        Document document,
        ConstructorDeclarationSyntax constructor,
        CancellationToken cancellationToken
    )
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || constructor.Parent is not ClassDeclarationSyntax containingClass)
        {
            return document;
        }

        if (
            constructor.Body is null
            || ConstructorInjectionAnalysisHelpers.HasMeaningfulBodyTrivia(constructor.Body)
        )
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var existingPropertyNames = containingClass
            .Members.OfType<PropertyDeclarationSyntax>()
            .Select(static property => property.Identifier.ValueText)
            .Aggregate(
                new HashSet<string>(StringComparer.Ordinal),
                static (set, name) =>
                {
                    set.Add(name);
                    return set;
                }
            );

        var propagation = ConstructorInjectionAnalysisHelpers.TryGetPropagatedParameterNames(
            constructor
        );
        if (!propagation.IsSafe)
        {
            return document;
        }

        var parametersToConvert = constructor
            .ParameterList.Parameters.Where(parameter =>
                !propagation.ParameterNames.Contains(parameter.Identifier.ValueText)
            )
            .ToImmutableArray();
        if (parametersToConvert.Length == 0)
        {
            return document;
        }

        var newProperties = parametersToConvert
            .Select(parameter =>
            {
                var propertyName = BuildUniquePropertyName(
                    ToPascalCase(parameter.Identifier.ValueText),
                    existingPropertyNames
                );

                return SyntaxFactory
                    .PropertyDeclaration(parameter.Type!, SyntaxFactory.Identifier(propertyName))
                    .WithAttributeLists(
                        SyntaxFactory.SingletonList(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList(
                                    BuildInjectAttribute(parameter, semanticModel, cancellationToken)
                                )
                            )
                        )
                    )
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.RequiredKeyword)
                        )
                    )
                    .WithAccessorList(
                        SyntaxFactory.AccessorList(
                            SyntaxFactory.List([
                                SyntaxFactory
                                    .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                                    ),
                                SyntaxFactory
                                    .AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                                    ),
                            ])
                        )
                    )
                    .WithAdditionalAnnotations(Formatter.Annotation);
            })
            .ToImmutableArray();

        SyntaxList<MemberDeclarationSyntax> updatedMembers;
        if (propagation.ParameterNames.Count == 0)
        {
            var newPropertiesWithTrivia = newProperties;
            newPropertiesWithTrivia =
            [
                newPropertiesWithTrivia[0].WithLeadingTrivia(constructor.GetLeadingTrivia()),
                .. newPropertiesWithTrivia.Skip(1),
            ];
            updatedMembers = containingClass
                .Members.Remove(constructor)
                .InsertRange(0, newPropertiesWithTrivia);
        }
        else
        {
            var propagatedParameters = constructor.ParameterList.Parameters.Where(parameter =>
                propagation.ParameterNames.Contains(parameter.Identifier.ValueText)
            );
            var updatedConstructor = constructor.WithParameterList(
                SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(propagatedParameters))
            );
            var constructorIndex = containingClass.Members.IndexOf(constructor);
            updatedMembers = containingClass.Members.Replace(constructor, updatedConstructor);
            updatedMembers = updatedMembers.InsertRange(constructorIndex, newProperties);
        }

        var updatedClass = containingClass.WithMembers(updatedMembers);
        var updatedRoot = root.ReplaceNode(
            containingClass,
            updatedClass.WithAdditionalAnnotations(Formatter.Annotation)
        );
        return document.WithSyntaxRoot(updatedRoot);
    }

    private static string BuildUniquePropertyName(
        string baseName,
        ISet<string> existingPropertyNames
    )
    {
        var name = baseName;
        var suffix = 1;
        while (!existingPropertyNames.Add(name))
        {
            name = $"{baseName}{suffix}";
            suffix++;
        }

        return name;
    }

    private static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "Dependency";
        }

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private static AttributeSyntax BuildInjectAttribute(
        ParameterSyntax parameter,
        SemanticModel? semanticModel,
        CancellationToken cancellationToken
    )
    {
        var keyExpression = GetFromKeyedServicesKeyExpression(parameter, semanticModel, cancellationToken);
        if (keyExpression is null)
        {
            return SyntaxFactory.Attribute(SyntaxFactory.ParseName("global::GenDI.Inject"));
        }

        return SyntaxFactory.Attribute(
            SyntaxFactory.ParseName("global::GenDI.Inject"),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(keyExpression)
                        .WithNameEquals(SyntaxFactory.NameEquals("Key"))
                )
            )
        );
    }

    private static ExpressionSyntax? GetFromKeyedServicesKeyExpression(
        ParameterSyntax parameter,
        SemanticModel? semanticModel,
        CancellationToken cancellationToken
    )
    {
        foreach (var attributeList in parameter.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (attribute.ArgumentList?.Arguments.Count is not > 0)
                {
                    continue;
                }

                var attributeType = semanticModel?.GetTypeInfo(attribute, cancellationToken).Type;
                if (
                    attributeType?.ToDisplayString()
                    != "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute"
                )
                {
                    continue;
                }

                return attribute.ArgumentList.Arguments[0].Expression;
            }
        }

        return null;
    }
}

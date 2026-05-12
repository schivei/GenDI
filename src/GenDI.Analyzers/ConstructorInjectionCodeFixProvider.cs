using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            || root.FindNode(diagnostic.Location.SourceSpan) is not ConstructorDeclarationSyntax constructor
        )
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Convert constructor injection to [Inject] properties",
                createChangedDocument: cancellationToken => ConvertAsync(
                    context.Document,
                    constructor,
                    cancellationToken
                ),
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

        var injectAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Inject"));

        var newProperties = constructor.ParameterList.Parameters.Select(parameter =>
        {
            var propertyName = BuildUniquePropertyName(
                ToPascalCase(parameter.Identifier.ValueText),
                existingPropertyNames
            );

            return SyntaxFactory
                .PropertyDeclaration(parameter.Type!, SyntaxFactory.Identifier(propertyName))
                .WithAttributeLists(
                    SyntaxFactory.SingletonList(
                        SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(injectAttribute))
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
                        SyntaxFactory.List(
                            [
                                SyntaxFactory
                                    .AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory
                                    .AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            ]
                        )
                    )
                );
        });

        var updatedClass = containingClass
            .RemoveNode(constructor, SyntaxRemoveOptions.KeepNoTrivia)!
            .WithMembers(containingClass.Members.Remove(constructor).InsertRange(0, newProperties));

        var updatedRoot = root.ReplaceNode(containingClass, updatedClass);
        return document.WithSyntaxRoot(updatedRoot);
    }

    private static string BuildUniquePropertyName(string baseName, ISet<string> existingPropertyNames)
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
        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }
}

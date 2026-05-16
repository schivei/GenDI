using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenDI.Analyzers;

internal static class ConstructorInjectionAnalysisHelpers
{
    public static PropagationAnalysisResult TryGetPropagatedParameterNames(
        ConstructorDeclarationSyntax constructorDeclaration
    )
    {
        if (constructorDeclaration.Initializer?.ArgumentList is null)
        {
            return new PropagationAnalysisResult(
                new HashSet<string>(StringComparer.Ordinal),
                isSafe: true
            );
        }

        if (!TryCollectPropagatedParameterNames(constructorDeclaration, out var parameterNames))
        {
            return new PropagationAnalysisResult(
                new HashSet<string>(StringComparer.Ordinal),
                isSafe: false
            );
        }

        return new PropagationAnalysisResult(parameterNames, isSafe: true);
    }

    public static bool HasMeaningfulBodyTrivia(BlockSyntax constructorBody)
    {
        static bool IsMeaningful(SyntaxTrivia trivia)
        {
            return trivia
                is not {
                    RawKind: (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.WhitespaceTrivia
                        or (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.EndOfLineTrivia
                };
        }

        return constructorBody.OpenBraceToken.TrailingTrivia.Any(IsMeaningful)
            || constructorBody.CloseBraceToken.LeadingTrivia.Any(IsMeaningful);
    }

    private static bool TryCollectPropagatedParameterNames(
        ConstructorDeclarationSyntax constructorDeclaration,
        out HashSet<string> propagatedParameterNames
    )
    {
        propagatedParameterNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var argument in constructorDeclaration.Initializer.ArgumentList.Arguments)
        {
            var propagatedParameterName = TryGetForwardedParameterName(argument.Expression);
            if (propagatedParameterName is null)
            {
                propagatedParameterNames.Clear();
                return false;
            }

            propagatedParameterNames.Add(propagatedParameterName);
        }

        return true;
    }

    private static string? TryGetForwardedParameterName(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesizedExpression:
                    expression = parenthesizedExpression.Expression;
                    continue;
                case IdentifierNameSyntax identifierName:
                    return identifierName.Identifier.ValueText;
                default:
                    return null;
            }
        }
    }
}

internal readonly struct PropagationAnalysisResult
{
    public PropagationAnalysisResult(HashSet<string> parameterNames, bool isSafe)
    {
        ParameterNames = parameterNames;
        IsSafe = isSafe;
    }

    public HashSet<string> ParameterNames { get; }

    public bool IsSafe { get; }
}

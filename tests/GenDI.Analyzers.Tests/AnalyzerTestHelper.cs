using System.Collections.Immutable;
using System.Reflection;
using GenDI.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GenDI.Analyzers.Tests;

internal static class AnalyzerTestHelper
{
    public static ImmutableArray<Diagnostic> Run(string userSource)
    {
        var source = $$"""
            using GenDI;
            using Microsoft.Extensions.DependencyInjection;

            {{userSource}}
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest)
        );

        var compilation = CSharpCompilation.Create(
            "AnalyzerTests",
            [syntaxTree],
            BuildReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [new InjectableUsageAnalyzer()],
            new CompilationWithAnalyzersOptions(
                analyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: false
            )
        );

        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var references = new List<MetadataReference>();
        var trustedPlatformAssemblies =
            AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        foreach (
            var path in trustedPlatformAssemblies.Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            references.Add(MetadataReference.CreateFromFile(path));
        }

        references.Add(MetadataReference.CreateFromFile(typeof(InjectableAttribute).Assembly.Location));
        references.Add(
            MetadataReference.CreateFromFile(
                Assembly.Load("Microsoft.Extensions.DependencyInjection.Abstractions").Location
            )
        );

        return references.ToImmutableArray();
    }
}

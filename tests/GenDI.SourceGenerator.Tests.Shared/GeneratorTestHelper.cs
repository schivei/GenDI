using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace GenDI.SourceGenerator.Tests;

internal static class GeneratorTestHelper
{
    public static string GenerateSource(string userSource, bool includeGeneratedCodeInCoverage)
    {
        var source = $$"""
            using GenDI;
            using Microsoft.Extensions.DependencyInjection;
            [assembly: GenDI.GenDICoveration({{includeGeneratedCodeInCoverage.ToString().ToLowerInvariant()}})]

            {{userSource}}
            """;

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CSharpCompilation.Create(
            assemblyName: "Consumer.Tests",
            syntaxTrees: new[] { syntaxTree },
            references: BuildReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator generator = new GenDISourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var generationErrors = diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(generationErrors);

        var result = driver.GetRunResult();
        var generated = result.Results
            .SelectMany(static runResult => runResult.GeneratedSources)
            .Single(static generatedSource => generatedSource.HintName == "GenDIServiceCollectionExtensions.g.cs");

        return generated.SourceText.ToString();
    }

    private static IEnumerable<MetadataReference> BuildReferences()
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty;
        var references = tpa
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(InjectableAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location));

        return references;
    }
}

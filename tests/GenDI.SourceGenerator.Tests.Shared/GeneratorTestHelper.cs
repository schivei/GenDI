using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace GenDI.SourceGenerator.Tests;

internal static class GeneratorTestHelper
{
    public static string GenerateSource(string userSource, bool? includeGeneratedCodeInCoverage)
    {
        return GenerateSourceWithAssemblyName("Consumer.Tests", userSource, includeGeneratedCodeInCoverage);
    }

    /// <summary>
    /// Asserts that the generator produces no output for the given source.
    /// Fails the test with an explicit message if any source is generated.
    /// </summary>
    public static void AssertNoSourceGenerated(string userSource, bool? includeGeneratedCodeInCoverage)
    {
        AssertNoSourceGeneratedWithAssemblyName("Consumer.Tests", userSource, includeGeneratedCodeInCoverage);
    }

    public static void AssertNoSourceGeneratedWithAssemblyName(
        string? assemblyName,
        string userSource,
        bool? includeGeneratedCodeInCoverage
    )
    {
        var assemblyCoverageAttribute = includeGeneratedCodeInCoverage.HasValue
            ? $"[assembly: GenDI.GenDICoveration({includeGeneratedCodeInCoverage.Value.ToString().ToLowerInvariant()})]"
            : string.Empty;

        var source = $$"""
            using GenDI;
            using Microsoft.Extensions.DependencyInjection;
            {{assemblyCoverageAttribute}}

            {{userSource}}
            """;

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: BuildReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        IIncrementalGenerator generator = CreateGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var generationErrors = diagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Empty(generationErrors);

        var result = driver.GetRunResult();
        var generatedSources = result
            .Results.SelectMany(static runResult => runResult.GeneratedSources)
            .Where(static generatedSource =>
                generatedSource.HintName == "GenDIServiceCollectionExtensions.g.cs"
            )
            .ToArray();

        Assert.Empty(generatedSources);
    }

    public static string GenerateSourceWithAssemblyName(
        string? assemblyName,
        string userSource,
        bool? includeGeneratedCodeInCoverage
    )
    {
        var assemblyCoverageAttribute = includeGeneratedCodeInCoverage.HasValue
            ? $"[assembly: GenDI.GenDICoveration({includeGeneratedCodeInCoverage.Value.ToString().ToLowerInvariant()})]"
            : string.Empty;

        var source = $$"""
            using GenDI;
            using Microsoft.Extensions.DependencyInjection;
            {{assemblyCoverageAttribute}}

            {{userSource}}
            """;

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: new[] { syntaxTree },
            references: BuildReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        IIncrementalGenerator generator = CreateGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var generationErrors = diagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Empty(generationErrors);

        var result = driver.GetRunResult();
        var generated = result
            .Results.SelectMany(static runResult => runResult.GeneratedSources)
            .Single(static generatedSource =>
                generatedSource.HintName == "GenDIServiceCollectionExtensions.g.cs"
            );

        return generated.SourceText.ToString();
    }

    private static IIncrementalGenerator CreateGenerator()
    {
        var assemblyPath = ResolveGeneratorAssemblyPath();
        var assembly = Assembly.LoadFrom(assemblyPath);
        var type = assembly.GetType(
            "GenDI.SourceGenerator.GenDISourceGenerator",
            throwOnError: true
        )!;
        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);
        return Assert.IsAssignableFrom<IIncrementalGenerator>(instance);
    }

    private static string ResolveGeneratorAssemblyPath()
    {
        var candidate = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "GenDI.SourceGenerator.dll")
        );
        if (File.Exists(candidate))
        {
            return candidate;
        }

        var buildConfigurations = new[] { "Debug", "Release" };
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            foreach (var configuration in buildConfigurations)
            {
                var rootCandidate = Path.Combine(
                    directory.FullName,
                    "src",
                    "GenDI.SourceGenerator",
                    "bin",
                    configuration,
                    "netstandard2.0",
                    "GenDI.SourceGenerator.dll"
                );
                if (File.Exists(rootCandidate))
                {
                    return rootCandidate;
                }
            }
        }

        throw new FileNotFoundException(
            "GenDI.SourceGenerator.dll not found for source-generator tests."
        );
    }

    private static IEnumerable<MetadataReference> BuildReferences()
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty;
        var references = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToList();

        references.Add(
            MetadataReference.CreateFromFile(typeof(InjectableAttribute).Assembly.Location)
        );
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location
            )
        );
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Options.IOptions<>).Assembly.Location
            )
        );
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Configuration.IConfiguration).Assembly.Location
            )
        );
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.Configuration.ConfigurationBinder).Assembly.Location
            )
        );

        return references;
    }
}

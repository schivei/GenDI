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
    public static Diagnostic[] GetGeneratorDiagnostics(
        string userSource,
        bool? includeGeneratedCodeInCoverage,
        params (string AssemblyName, string Source)[] referencedAssemblies
    )
    {
        var (_, diagnostics) = RunGenerator(
            "Consumer.Tests",
            userSource,
            includeGeneratedCodeInCoverage,
            referencedAssemblies
        );
        return diagnostics;
    }

    public static string GenerateSource(
        string userSource,
        bool? includeGeneratedCodeInCoverage,
        params (string AssemblyName, string Source)[] referencedAssemblies
    )
    {
        return GenerateSourceWithAssemblyName(
            "Consumer.Tests",
            userSource,
            includeGeneratedCodeInCoverage,
            referencedAssemblies
        );
    }

    /// <summary>
    /// Asserts that the generator produces no output for the given source.
    /// Fails the test with an explicit message if any source is generated.
    /// </summary>
    public static void AssertNoSourceGenerated(
        string userSource,
        bool? includeGeneratedCodeInCoverage,
        params (string AssemblyName, string Source)[] referencedAssemblies
    )
    {
        AssertNoSourceGeneratedWithAssemblyName(
            "Consumer.Tests",
            userSource,
            includeGeneratedCodeInCoverage,
            referencedAssemblies
        );
    }

    public static void AssertNoSourceGeneratedWithAssemblyName(
        string? assemblyName,
        string userSource,
        bool? includeGeneratedCodeInCoverage,
        params (string AssemblyName, string Source)[] referencedAssemblies
    )
    {
        var (driver, diagnostics) = RunGenerator(
            assemblyName,
            userSource,
            includeGeneratedCodeInCoverage,
            referencedAssemblies
        );

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
        bool? includeGeneratedCodeInCoverage,
        params (string AssemblyName, string Source)[] referencedAssemblies
    )
    {
        var (driver, diagnostics) = RunGenerator(
            assemblyName,
            userSource,
            includeGeneratedCodeInCoverage,
            referencedAssemblies
        );

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

    private static (GeneratorDriver Driver, Diagnostic[] Diagnostics) RunGenerator(
        string? assemblyName,
        string userSource,
        bool? includeGeneratedCodeInCoverage,
        params (string AssemblyName, string Source)[] referencedAssemblies
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
            references: BuildReferences(referencedAssemblies),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        IIncrementalGenerator generator = CreateGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        var result = driver.GetRunResult();
        var generatorDiagnostics = result
            .Results.SelectMany(static runResult => runResult.Diagnostics)
            .ToArray();
        return (driver, generatorDiagnostics);
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
        return Assert.IsType<IIncrementalGenerator>(instance, exactMatch: false);
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

    private static List<PortableExecutableReference> BuildReferences(
        params (string AssemblyName, string Source)[] referencedAssemblies
    )
    {
        var tpa = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string) ?? string.Empty;
        var references = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .OfType<PortableExecutableReference>()
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

        foreach (var referencedAssembly in referencedAssemblies)
        {
            references.Add(BuildReferencedAssembly(referencedAssembly, references));
        }

        return references;
    }

    private static PortableExecutableReference BuildReferencedAssembly(
        (string AssemblyName, string Source) referencedAssembly,
        IReadOnlyCollection<PortableExecutableReference> references
    )
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var wrappedSource =
            $$"""
            using GenDI;
            using Microsoft.Extensions.DependencyInjection;

            {{referencedAssembly.Source}}
            """;
        var compilation = CSharpCompilation.Create(
            assemblyName: referencedAssembly.AssemblyName,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(wrappedSource, parseOptions) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        Assert.True(
            emitResult.Success,
            string.Join(Environment.NewLine, emitResult.Diagnostics.Select(diagnostic => diagnostic.ToString()))
        );

        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;

[assembly: GenDI.GenDICoveration(false)]

namespace GenDI.Analyzers.Tests;

internal static class AnalyzerTestHelper
{
    public static ImmutableArray<Diagnostic> Run(string userSource)
    {
        var source = $"""
            using System;
            using GenDI;
            using Microsoft.Extensions.DependencyInjection;

            {userSource}
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

        references.Add(
            MetadataReference.CreateFromFile(typeof(InjectableAttribute).Assembly.Location)
        );
        references.Add(MetadataReference.CreateFromFile(typeof(ServiceLifetime).Assembly.Location));

        return [.. references];
    }

    public static async Task<string> ApplyConstructorInjectionCodeFixAsync(string userSource)
    {
        var source = $"""
            using System;
            using GenDI;
            using Microsoft.Extensions.DependencyInjection;

            {userSource}
            """;

        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace
            .CurrentSolution.AddProject(
                ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    "AnalyzerTests",
                    "AnalyzerTests",
                    LanguageNames.CSharp,
                    parseOptions: CSharpParseOptions.Default.WithLanguageVersion(
                        LanguageVersion.Latest
                    ),
                    compilationOptions: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary
                    ),
                    metadataReferences: BuildReferences()
                )
            )
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        var document = solution.GetDocument(documentId)!;
        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var diagnostics = await compilation!
            .WithAnalyzers([new InjectableUsageAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);

        var targetDiagnostic = diagnostics.Single(static diagnostic => diagnostic.Id == "GENDI003");
        var codeFixProvider = new ConstructorInjectionCodeFixProvider();
        var actions = new List<CodeAction>();

        var context = new CodeFixContext(
            document,
            targetDiagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None
        );

        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        var operations = await actions[0]
            .GetOperationsAsync(CancellationToken.None)
            .ConfigureAwait(false);
        var applyOperation = operations.OfType<ApplyChangesOperation>().Single();
        var changedDocument = applyOperation.ChangedSolution.GetDocument(documentId)!;
        var changedText = await changedDocument.GetTextAsync().ConfigureAwait(false);
        return changedText.ToString();
    }
}

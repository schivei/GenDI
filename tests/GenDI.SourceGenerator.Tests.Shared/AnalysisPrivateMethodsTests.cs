using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace GenDI.SourceGenerator.Tests;

public class AnalysisPrivateMethodsTests
{
    private static Assembly LoadGeneratorAssembly()
    {
        var candidate = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "GenDI.SourceGenerator.dll")
        );
        if (File.Exists(candidate))
        {
            var bytes = File.ReadAllBytes(candidate);

            return Assembly.Load(bytes);
        }

        throw new FileNotFoundException("GenDI.SourceGenerator.dll not found for tests.");
    }

    private static CSharpCompilation BuildCompilation(
        string source,
        params MetadataReference[] extraReferences
    )
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        var refs = tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => MetadataReference.CreateFromFile(p))
            .ToList();

        if (extraReferences is not null)
        {
            refs.AddRange(extraReferences.Select(r => (PortableExecutableReference)r));
        }

        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }

    [Fact]
    public void LifetimePriority_returns_expected_values()
    {
        var asm = LoadGeneratorAssembly();
        var type = asm.GetType("GenDI.SourceGenerator.GenDISourceGenerator", throwOnError: true)!;
        var method = type.GetMethod(
            "LifetimePriority",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;

        Assert.Equal(3, (int)method.Invoke(null, ["ServiceLifetime.Scoped"])!);
        Assert.Equal(2, (int)method.Invoke(null, ["ServiceLifetime.Singleton"])!);
        Assert.Equal(1, (int)method.Invoke(null, ["ServiceLifetime.Transient"])!);
    }

    [Fact]
#pragma warning disable S3776
    public void IsClosedType_and_IsClosedTypeArgument_behave_as_expected()
#pragma warning restore S3776
    {
        var source =
            @"
namespace TestCases
{
    public class Generic<T> { }

    public class Closed : Generic<int> { }

    public class OpenGeneric<T> : Generic<T> { }
}
";

        var compilation = BuildCompilation(source);
        var closed = compilation.GetTypeByMetadataName("TestCases.Closed");
        var openGeneric = compilation.GetTypeByMetadataName("TestCases.OpenGeneric");
        var generic = compilation.GetTypeByMetadataName("TestCases.Generic");

        // Fallback: if Roslyn doesn't resolve by metadata name in this environment,
        // attempt to discover declared types via the syntax tree semantic model.
        if (closed is null || openGeneric is null || generic is null)
        {
            var tree = compilation.SyntaxTrees.First();
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot(TestContext.Current.CancellationToken);

            if (closed is null)
            {
                var closedDecl = root.DescendantNodes()
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.ValueText == "Closed");
                closed = closedDecl is null
                    ? null
                    : model.GetDeclaredSymbol(closedDecl, TestContext.Current.CancellationToken);
            }

            if (openGeneric is null)
            {
                var openDecl = root.DescendantNodes()
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.ValueText == "OpenGeneric");
                openGeneric = openDecl is null
                    ? null
                    : model.GetDeclaredSymbol(openDecl, TestContext.Current.CancellationToken);
            }

            if (generic is null)
            {
                var genericDecl = root.DescendantNodes()
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.ValueText == "Generic");
                generic = genericDecl is null
                    ? null
                    : model.GetDeclaredSymbol(genericDecl, TestContext.Current.CancellationToken);
            }
        }

        Assert.NotNull(closed);
        Assert.NotNull(openGeneric);
        Assert.NotNull(generic);

        var asm = LoadGeneratorAssembly();
        var type = asm.GetType("GenDI.SourceGenerator.GenDISourceGenerator", throwOnError: true)!;
        var isClosed = type.GetMethod(
            "IsClosedType",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;
        var isClosedArg = type.GetMethod(
            "IsClosedTypeArgument",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;

        // Closed concrete type -> true
        Assert.True((bool)isClosed.Invoke(null, [closed])!);

        // Open generic type symbol (has type parameter) -> false
        Assert.False((bool)isClosed.Invoke(null, [openGeneric])!);

        // Primitive type argument should be considered closed
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);
        Assert.True((bool)isClosedArg.Invoke(null, [intType])!);

        // Type parameter (from generic) is not a closed type argument
        var tParam = openGeneric.TypeParameters.First();
        Assert.False((bool)isClosedArg.Invoke(null, [tParam])!);
    }

    [Fact]
    public void IsDeclaredSymbolAccessibleFromGeneratedCode_respects_internal_visibility_across_assemblies()
    {
        var referencedSource =
            @"
namespace ReferencedLib
{
    internal class Hidden
    {
    }

    public class PublicType
    {
    }
}
";

        // Build referenced assembly and get metadata reference
        var referencedCompilation = BuildCompilation(referencedSource);
        using var ms = new MemoryStream();
        var emit = referencedCompilation.Emit(
            ms,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.True(emit.Success, string.Join("\n", emit.Diagnostics.Select(d => d.ToString())));
        ms.Position = 0;
        var metadataRef = MetadataReference.CreateFromImage(ms.ToArray());

        var consumerSource =
            @"
namespace Consumer
{
    public class UsesReferenced
    {
    }
}
";

        var compilation = BuildCompilation(consumerSource, metadataRef);

        var referencedHidden = compilation.GetTypeByMetadataName("ReferencedLib.Hidden");
        var referencedPublic = compilation.GetTypeByMetadataName("ReferencedLib.PublicType");

        Assert.NotNull(referencedHidden);
        Assert.NotNull(referencedPublic);

        var asm = LoadGeneratorAssembly();
        var type = asm.GetType("GenDI.SourceGenerator.GenDISourceGenerator", throwOnError: true)!;
        var method = type.GetMethod(
            "IsDeclaredSymbolAccessibleFromGeneratedCode",
            BindingFlags.NonPublic | BindingFlags.Static
        )!;

        // Public symbol from referenced assembly -> accessible
        Assert.True((bool)method.Invoke(null, [referencedPublic, compilation])!);

        // Internal symbol from referenced assembly -> not accessible from this compilation
        Assert.False((bool)method.Invoke(null, [referencedHidden, compilation])!);
    }
}

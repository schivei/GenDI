using System.Threading.Tasks;
using System.Reflection;
using GenDI.Analyzers;
using Xunit;

namespace GenDI.Analyzers.Tests;

public class InjectableUsageAnalyzerTests
{
    [Fact]
    public void Injectable_on_abstract_class_reports_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public abstract class AbstractService
            {
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI002");
    }

    [Fact]
    public void Inject_on_non_init_property_reports_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class InvalidInjectProperty
            {
                [Inject]
                public object Service { get; set; } = default!;
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI001");
    }

    [Fact]
    public void Inject_on_init_property_does_not_report_non_init_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class ValidInjectProperty
            {
                [Inject]
                public required object Service { get; init; }
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI001");
    }

    [Fact]
    public void Concrete_injectable_class_does_not_report_concrete_type_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class ConcreteService
            {
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI002");
    }

    [Fact]
    public void Injectable_constructor_injection_reports_code_fix_hint()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class ConstructorInjectedService
            {
                public ConstructorInjectedService(IServiceProvider serviceProvider)
                {
                }
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI003");
    }

    [Fact]
    public void Constructor_with_logic_does_not_report_code_fix_hint()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class ConstructorWithLogicService
            {
                public ConstructorWithLogicService(IServiceProvider serviceProvider)
                {
                    _ = serviceProvider;
                }
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI003");
    }

    [Fact]
    public void Non_public_constructor_does_not_report_code_fix_hint()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class InternalConstructorService
            {
                internal InternalConstructorService(IServiceProvider serviceProvider)
                {
                }
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI003");
    }

    [Fact]
    public async Task Code_fix_converts_constructor_injection_to_inject_properties()
    {
        var fixedSource = await AnalyzerTestHelper.ApplyConstructorInjectionCodeFixAsync(
            """
            [Injectable]
            public sealed class ConstructorInjectedService
            {
                public ConstructorInjectedService(IServiceProvider serviceProvider, IFormatProvider formatProvider)
                {
                }
            }
            """
        );

        Assert.DoesNotContain("public ConstructorInjectedService(", fixedSource);
        Assert.Contains("[Inject]", fixedSource);
        Assert.Contains("public required IServiceProvider ServiceProvider { get; init; }", fixedSource);
        Assert.Contains("public required IFormatProvider FormatProvider { get; init; }", fixedSource);
    }

    [Fact]
    public async Task Code_fix_avoids_property_name_collisions()
    {
        var fixedSource = await AnalyzerTestHelper.ApplyConstructorInjectionCodeFixAsync(
            """
            [Injectable]
            public sealed class ConstructorInjectedService
            {
                public required IServiceProvider ServiceProvider { get; init; }

                public ConstructorInjectedService(IServiceProvider serviceProvider)
                {
                }
            }
            """
        );

        Assert.Contains("public required IServiceProvider ServiceProvider1 { get; init; }", fixedSource);
    }

    [Fact]
    public void Code_fix_pascal_case_fallback_handles_empty_value()
    {
        var method = typeof(ConstructorInjectionCodeFixProvider).GetMethod(
            "ToPascalCase",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        var result = (string?)method!.Invoke(null, [""]);
        Assert.Equal("Dependency", result);
    }
}

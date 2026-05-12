using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
    public void Diagnostics_expose_help_links_for_documentation_index()
    {
        var nonInitDiagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class InvalidInjectProperty
            {
                [Inject]
                public object Service { get; set; } = default!;
            }
            """
        );
        var abstractTypeDiagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public abstract class AbstractService
            {
            }
            """
        );
        var constructorDiagnostics = AnalyzerTestHelper.Run(
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

        var diagnostic001 = Assert.Single(
            nonInitDiagnostics.Where(static diagnostic => diagnostic.Id == "GENDI001")
        );
        var diagnostic002 = Assert.Single(
            abstractTypeDiagnostics.Where(static diagnostic => diagnostic.Id == "GENDI002")
        );
        var diagnostic003 = Assert.Single(
            constructorDiagnostics.Where(static diagnostic => diagnostic.Id == "GENDI003")
        );

        Assert.Equal(
            "https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md#gendi001---inject-attribute-requires-init-only-property",
            diagnostic001.Descriptor.HelpLinkUri
        );
        Assert.Equal(
            "https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md#gendi002---injectable-attribute-requires-concrete-class",
            diagnostic002.Descriptor.HelpLinkUri
        );
        Assert.Equal(
            "https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md#gendi003---constructor-injection-can-be-converted-to-gendi-property-injection",
            diagnostic003.Descriptor.HelpLinkUri
        );
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
    public void Constructor_with_only_base_propagation_does_not_report_code_fix_hint()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            public abstract class BaseService
            {
                protected BaseService(IServiceProvider serviceProvider)
                {
                }
            }

            [Injectable]
            public sealed class DerivedService : BaseService
            {
                public DerivedService(IServiceProvider serviceProvider) : base(serviceProvider)
                {
                }
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI003");
    }

    [Fact]
    public void Constructor_with_partial_base_propagation_reports_code_fix_hint()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            public abstract class BaseService
            {
                protected BaseService(IServiceProvider serviceProvider)
                {
                }
            }

            [Injectable]
            public sealed class DerivedService : BaseService
            {
                public DerivedService(IServiceProvider serviceProvider, IFormatProvider formatProvider) : base(serviceProvider)
                {
                }
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI003");
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
        Assert.Contains("[global::GenDI.Inject]", fixedSource);
        Assert.Contains(
            "public required IServiceProvider ServiceProvider { get; init; }",
            fixedSource
        );
        Assert.Contains(
            "public required IFormatProvider FormatProvider { get; init; }",
            fixedSource
        );
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

        Assert.Contains(
            "public required IServiceProvider ServiceProvider1 { get; init; }",
            fixedSource
        );
    }

    [Fact]
    public async Task Code_fix_converts_only_non_propagated_parameters()
    {
        var fixedSource = await AnalyzerTestHelper.ApplyConstructorInjectionCodeFixAsync(
            """
            public abstract class BaseService
            {
                protected BaseService(IServiceProvider serviceProvider)
                {
                }
            }

            [Injectable]
            public sealed class DerivedService : BaseService
            {
                public DerivedService(IServiceProvider serviceProvider, IFormatProvider formatProvider) : base(serviceProvider)
                {
                }
            }
            """
        );

        Assert.Contains(
            "public required IFormatProvider FormatProvider { get; init; }",
            fixedSource
        );
        Assert.DoesNotContain(
            "public required IServiceProvider ServiceProvider { get; init; }",
            fixedSource
        );
        Assert.Contains(
            "public DerivedService(IServiceProvider serviceProvider) : base(serviceProvider)",
            fixedSource
        );
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

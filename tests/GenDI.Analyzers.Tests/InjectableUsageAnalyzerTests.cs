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
    public void Inject_on_private_non_init_property_does_not_report_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class PrivateInjectProperty
            {
                [Inject]
                private object Service { get; set; } = default!;
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI001");
    }

    [Fact]
    public void Inject_on_protected_non_init_property_does_not_report_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public class ProtectedInjectProperty
            {
                [Inject]
                protected object Service { get; set; } = default!;
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI001");
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
    public void Generic_injectable_on_abstract_class_reports_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            public interface IServiceContract
            {
            }

            [Injectable<IServiceContract>]
            public abstract class AbstractService : IServiceContract
            {
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI002");
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
        var unresolvedDecoratorDiagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public interface IServiceContract
            {
            }

            [DecoratorFor<IServiceContract>]
            public sealed class InvalidDecorator : IServiceContract
            {
            }
            """
        );
        var inferredDecoratorDiagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public interface IServiceContract
            {
            }

            [DecoratorFor]
            public sealed class InvalidDecorator : IServiceContract
            {
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
        var inferredDecoratorInnerDiagnostic = Assert.Single(
            inferredDecoratorDiagnostics.Where(static diagnostic => diagnostic.Id == "GENDI005")
        );
        var explicitDecoratorInnerDiagnostic = Assert.Single(
            unresolvedDecoratorDiagnostics.Where(static diagnostic => diagnostic.Id == "GENDI005")
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
        Assert.Equal(
            "https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md#gendi005---decorator-requires-an-inner-dependency",
            inferredDecoratorInnerDiagnostic.Descriptor.HelpLinkUri
        );
        Assert.Equal(
            "https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md#gendi005---decorator-requires-an-inner-dependency",
            explicitDecoratorInnerDiagnostic.Descriptor.HelpLinkUri
        );
    }

    [Fact]
    public void Decorator_without_inner_dependency_reports_error()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public interface IServiceContract
            {
            }

            [DecoratorFor<IServiceContract>]
            public sealed class InvalidDecorator : IServiceContract
            {
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI005");
    }

    [Fact]
    public void Non_generic_decorator_without_single_service_contract_reports_error()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [DecoratorFor]
            public sealed class InvalidDecorator
            {
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI004");
    }

    [Fact]
    public void Non_generic_decorator_with_matching_inner_dependency_does_not_report_error()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public interface IServiceContract
            {
            }

            [DecoratorFor]
            public sealed class ValidDecorator(IServiceContract inner) : IServiceContract
            {
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI004");
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI005");
    }

    [Fact]
    public void Non_generic_decorator_can_infer_closed_service_injection_base_contract()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public abstract class ServiceContractBase
            {
            }

            [DecoratorFor]
            public sealed class ValidDecorator(ServiceContractBase inner) : ServiceContractBase
            {
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI004");
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI005");
    }

    [Fact]
    public void Decorator_with_matching_inject_init_property_does_not_report_error()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public interface IServiceContract
            {
            }

            [DecoratorFor<IServiceContract>]
            public sealed class ValidDecorator : IServiceContract
            {
                [Inject]
                public required IServiceContract Inner { get; init; }
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI005");
    }

    [Fact]
    public void Non_generic_decorator_with_only_open_generic_contract_reports_resolvable_contract_error()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public interface IServiceContract<T>
            {
            }

            [DecoratorFor]
            public sealed class InvalidDecorator<T>(IServiceContract<T> inner) : IServiceContract<T>
            {
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI004");
    }

    [Fact]
    public void Decorator_inner_dependency_must_match_generator_selected_constructor()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [ServiceInjection]
            public interface IServiceContract
            {
            }

            [DecoratorFor<IServiceContract>]
            public sealed class InvalidDecorator : IServiceContract
            {
                public InvalidDecorator(IServiceContract inner)
                {
                }

                public InvalidDecorator(IServiceProvider serviceProvider, IDisposable dependency)
                {
                }
            }
            """
        );

        Assert.Contains(diagnostics, static diagnostic => diagnostic.Id == "GENDI005");
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
    public void Constructor_with_only_base_propagation_does_not_report_gendi003_diagnostic()
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
    public void Constructor_with_partial_base_propagation_reports_gendi003_diagnostic()
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
    public void Constructor_with_unsupported_base_propagation_does_not_report_gendi003_diagnostic()
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
                public DerivedService(IServiceProvider serviceProvider, IFormatProvider formatProvider) : base((IServiceProvider)serviceProvider)
                {
                }
            }
            """
        );

        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Id == "GENDI003");
    }

    [Fact]
    public void Constructor_with_body_comment_does_not_report_gendi003_diagnostic()
    {
        var diagnostics = AnalyzerTestHelper.Run(
            """
            [Injectable]
            public sealed class ConstructorInjectedService
            {
                public ConstructorInjectedService(IServiceProvider serviceProvider)
                {
                    // Keep this constructor for docs
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
    public async Task Code_fix_keeps_keyed_service_metadata_when_present()
    {
        var fixedSource = await AnalyzerTestHelper.ApplyConstructorInjectionCodeFixAsync(
            """
            [Injectable]
            public sealed class ConstructorInjectedService
            {
                public ConstructorInjectedService([FromKeyedServices("my-key")] IServiceProvider serviceProvider)
                {
                }
            }
            """
        );

        Assert.Contains("[global::GenDI.Inject(Key = \"my-key\")]", fixedSource);
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

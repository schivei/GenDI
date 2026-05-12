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
}

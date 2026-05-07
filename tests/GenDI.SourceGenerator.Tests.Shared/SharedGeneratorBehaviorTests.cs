using System;
using Xunit;

namespace GenDI.SourceGenerator.Tests;

public class SharedGeneratorBehaviorTests
{
    [Fact]
    public void Generated_extension_respects_coverage_toggle()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            [Injectable]
            public sealed class SimpleService
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute);

        if (TestSettings.IncludeGeneratedCodeInCoverageAttribute is false)
        {
            Assert.Contains("[ExcludeFromCodeCoverage]", generatedSource, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain("[ExcludeFromCodeCoverage]", generatedSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Registers_serviceType_alongside_serviceInjection_contracts_and_generates_complex_factory()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace Contracts;

            [ServiceInjection]
            public interface IPipelineContract
            {
            }

            [ServiceInjection]
            public abstract class PipelineBase
            {
            }

            public interface IExplicitContract
            {
            }

            public interface IDependency
            {
            }

            public interface IPropertyDependency
            {
            }

            [Injectable<IExplicitContract>(ServiceLifetime.Scoped, Group = 1, Order = 2)]
            public sealed class ComplexService : PipelineBase, IPipelineContract
            {
                [Inject]
                internal required IPropertyDependency PropertyDependency { get; init; }

                public ComplexService(IDependency dependency)
                {
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute);

        Assert.Contains("services.AddScoped<global::Contracts.IExplicitContract>", generatedSource, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<global::Contracts.IPipelineContract>", generatedSource, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<global::Contracts.PipelineBase>", generatedSource, StringComparison.Ordinal);
        Assert.Contains("new global::Contracts.ComplexService(serviceProvider.GetRequiredService<global::Contracts.IDependency>())", generatedSource, StringComparison.Ordinal);
        Assert.Contains("@PropertyDependency = serviceProvider.GetRequiredService<global::Contracts.IPropertyDependency>()", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Orders_registrations_by_group_order_and_service_name()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace Ordering;

            [ServiceInjection]
            public interface IA
            {
            }

            [ServiceInjection]
            public interface IB
            {
            }

            [ServiceInjection]
            public interface IC
            {
            }

            [Injectable(Group = 1, Order = 2)]
            public sealed class ServiceB : IB
            {
            }

            [Injectable(Group = 1, Order = 2)]
            public sealed class ServiceA : IA
            {
            }

            [Injectable(Group = 0, Order = 9)]
            public sealed class ServiceC : IC
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute);

        var indexC = generatedSource.IndexOf("services.AddTransient<global::Ordering.IC>", StringComparison.Ordinal);
        var indexA = generatedSource.IndexOf("services.AddTransient<global::Ordering.IA>", StringComparison.Ordinal);
        var indexB = generatedSource.IndexOf("services.AddTransient<global::Ordering.IB>", StringComparison.Ordinal);

        Assert.True(indexC >= 0 && indexA >= 0 && indexB >= 0);
        Assert.True(indexC < indexA);
        Assert.True(indexA < indexB);
    }
}

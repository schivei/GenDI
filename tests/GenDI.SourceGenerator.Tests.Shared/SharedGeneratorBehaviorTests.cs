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
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        if (TestSettings.IncludeGeneratedCodeInCoverageAttribute is false)
        {
            Assert.Contains("[ExcludeFromCodeCoverage]", generatedSource, StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain(
                "[ExcludeFromCodeCoverage]",
                generatedSource,
                StringComparison.Ordinal
            );
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
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddScoped<global::Contracts.IExplicitContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddScoped<global::Contracts.IPipelineContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddScoped<global::Contracts.PipelineBase>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.True(
            generatedSource.Contains(
                "new global::Contracts.ComplexService(serviceProvider.GetRequiredService<global::Contracts.IDependency>())",
                StringComparison.Ordinal
            )
                || generatedSource.Contains(
                    "new global::Contracts.ComplexService(serviceProvider.GetService<global::Contracts.IDependency>())",
                    StringComparison.Ordinal
                )
        );
        Assert.True(
            generatedSource.Contains(
                "@PropertyDependency = serviceProvider.GetRequiredService<global::Contracts.IPropertyDependency>()",
                StringComparison.Ordinal
            )
                || generatedSource.Contains(
                    "@PropertyDependency = serviceProvider.GetService<global::Contracts.IPropertyDependency>()",
                    StringComparison.Ordinal
                )
        );
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
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var indexC = generatedSource.IndexOf(
            "services.AddTransient<global::Ordering.IC>",
            StringComparison.Ordinal
        );
        var indexA = generatedSource.IndexOf(
            "services.AddTransient<global::Ordering.IA>",
            StringComparison.Ordinal
        );
        var indexB = generatedSource.IndexOf(
            "services.AddTransient<global::Ordering.IB>",
            StringComparison.Ordinal
        );

        Assert.True(indexC >= 0 && indexA >= 0 && indexB >= 0);
        Assert.True(indexC < indexA);
        Assert.True(indexA < indexB);
    }

    [Fact]
    public void Supports_keyed_registration_and_resolution()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace Keyed;

            [ServiceInjection]
            public interface IContract
            {
            }

            public interface IDependency
            {
            }

            public interface IPropertyDependency
            {
            }

            [Injectable<IContract>(ServiceLifetime.Scoped, Key = "main")]
            public sealed class KeyedService([FromKeyedServices("dep")] IDependency dependency) : IContract
            {
                [Inject(Key = "prop")]
                public required IPropertyDependency PropertyDependency { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddKeyedScoped<global::Keyed.IContract>(\"main\"",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.True(
            generatedSource.Contains(
                "serviceProvider.GetRequiredKeyedService<global::Keyed.IDependency>(\"dep\")",
                StringComparison.Ordinal
            )
                || generatedSource.Contains(
                    "serviceProvider.GetKeyedService<global::Keyed.IDependency>(\"dep\")",
                    StringComparison.Ordinal
                )
        );
        Assert.True(
            generatedSource.Contains(
                "serviceProvider.GetRequiredKeyedService<global::Keyed.IPropertyDependency>(\"prop\")",
                StringComparison.Ordinal
            )
                || generatedSource.Contains(
                    "serviceProvider.GetKeyedService<global::Keyed.IPropertyDependency>(\"prop\")",
                    StringComparison.Ordinal
                )
        );
    }

    [Fact]
    public void Supports_global_qualified_injectable_attribute()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace GlobalQualified;

            [global::GenDI.Injectable]
            public sealed class GlobalQualifiedService
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::GlobalQualified.GlobalQualifiedService>(",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Explicit_contract_prevents_concrete_fallback()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ExplicitOnly;

            public interface IOnlyContract
            {
            }

            [Injectable<IOnlyContract>(ServiceLifetime.Singleton)]
            public sealed class ExplicitContractService
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddSingleton<global::ExplicitOnly.IOnlyContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "services.AddSingleton<global::ExplicitOnly.ExplicitContractService>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Uses_optional_resolution_for_nullable_or_oblivious_dependencies()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            #nullable disable
            namespace NullableCases;

            public interface IDependency
            {
            }

            [Injectable]
            public sealed class OptionalCtor(IDependency dependency)
            {
            }

            #nullable enable
            [Injectable]
            public sealed class OptionalProperty
            {
                [Inject]
                public IDependency? Dependency { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "new global::NullableCases.OptionalCtor(serviceProvider.GetService<global::NullableCases.IDependency>())",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "@Dependency = serviceProvider.GetService<global::NullableCases.IDependency>()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Escapes_special_key_literals()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace LiteralKeys;

            [ServiceInjection]
            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = double.NaN)]
            public sealed class NaNKeyedService
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = "line1\nline2\tvalue")]
            public sealed class EscapedStringKeyedService
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddKeyedSingleton<global::LiteralKeys.IContract>(double.NaN",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddKeyedSingleton<global::LiteralKeys.IContract>(\"line1\\nline2\\tvalue\"",
            generatedSource,
            StringComparison.Ordinal
        );
    }
}

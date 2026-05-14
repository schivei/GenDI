using System;
using Microsoft.CodeAnalysis;
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
                internal IPropertyDependency PropertyDependency { get; init; }

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

    [Fact]
    public void InjectOptional_uses_optional_resolution_even_for_non_nullable_property()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionalProperty;

            public interface IDependency
            {
            }

            [Injectable]
            public sealed class ServiceWithOptionalDependency
            {
                [InjectOptional]
                public required IDependency Dependency { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "@Dependency = serviceProvider.GetService<global::OptionalProperty.IDependency>()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ServiceInjection_lifetime_is_used_as_fallback_when_injectable_is_transient()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ContractLifetime;

            [ServiceInjection(ServiceLifetime.Scoped)]
            public interface IContract
            {
            }

            [Injectable]
            public sealed class ContractService : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddScoped<global::ContractLifetime.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Injectable_lifetime_takes_precedence_over_ServiceInjection_fallback()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ContractLifetimePriority;

            [ServiceInjection(ServiceLifetime.Scoped)]
            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton)]
            public sealed class ContractService : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddSingleton<global::ContractLifetimePriority.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ConditionalInjectable_wraps_registration_with_environment_guard()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace Conditional;

            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton)]
            [ConditionalInjectable("Development")]
            public sealed class ConditionalService : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "Environment.GetEnvironmentVariable(\"DOTNET_ENVIRONMENT\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Environment.GetEnvironmentVariable(\"ASPNETCORE_ENVIRONMENT\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("\"Development\"", generatedSource, StringComparison.Ordinal);
        Assert.Contains(
            "services.AddSingleton<global::Conditional.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void DecoratorFor_rewrites_service_registration_with_decorator_factory()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace DecoratorCase;

            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton)]
            public sealed class BaseContract : IContract
            {
            }

            [DecoratorFor<IContract>]
            public sealed class LoggingDecorator(IContract inner) : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "new global::DecoratorCase.LoggingDecorator((new global::DecoratorCase.BaseContract()))",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void DecoratorFor_builds_ordered_pipeline_and_infers_non_generic_contract()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace DecoratorPipeline;

            [ServiceInjection]
            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton)]
            public sealed class BaseContract : IContract
            {
            }

            [DecoratorFor<IContract>(Order = 0)]
            public sealed class LoggingDecorator(IContract inner) : IContract
            {
            }

            [DecoratorFor(Order = 1)]
            public sealed class ValidationDecorator : IContract
            {
                [Inject]
                public required IContract Inner { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "new global::DecoratorPipeline.ValidationDecorator()",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "@Inner = (new global::DecoratorPipeline.LoggingDecorator((new global::DecoratorPipeline.BaseContract())))",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void DecoratorFor_applies_pipeline_only_to_final_unkeyed_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace DecoratorMulti;

            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton)]
            public sealed class FirstImplementation : IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton)]
            public sealed class SecondImplementation : IContract
            {
            }

            [DecoratorFor<IContract>]
            public sealed class LoggingDecorator(IContract inner) : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "new global::DecoratorMulti.FirstImplementation()",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "new global::DecoratorMulti.LoggingDecorator((new global::DecoratorMulti.SecondImplementation()))",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Equal(
            1,
            generatedSource.Split(
                    "new global::DecoratorMulti.LoggingDecorator(",
                    StringSplitOptions.None
                )
                .Length - 1
        );
    }

    [Fact]
    public void DecoratorFor_uses_injectable_order_as_fallback_when_decorator_order_is_omitted()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace DecoratorOrderFallback;

            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton)]
            public sealed class BaseContract : IContract
            {
            }

            [Injectable(Order = 1)]
            [DecoratorFor<IContract>]
            public sealed class LoggingDecorator(IContract inner) : IContract
            {
            }

            [Injectable(Order = 2)]
            [DecoratorFor<IContract>]
            public sealed class ValidationDecorator(IContract inner) : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var loggingIndex = generatedSource.IndexOf(
            "new global::DecoratorOrderFallback.LoggingDecorator((new global::DecoratorOrderFallback.BaseContract()))",
            StringComparison.Ordinal
        );
        var validationIndex = generatedSource.IndexOf(
            "new global::DecoratorOrderFallback.ValidationDecorator(",
            StringComparison.Ordinal
        );

        Assert.True(loggingIndex >= 0);
        Assert.True(validationIndex >= 0);
        Assert.True(validationIndex < loggingIndex);
        Assert.Contains(
            "new global::DecoratorOrderFallback.LoggingDecorator((new global::DecoratorOrderFallback.BaseContract()))",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "new global::DecoratorOrderFallback.ValidationDecorator(",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Inject_lifetime_override_is_used_for_indirect_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace IndirectLifetime;

            public interface IContract
            {
            }

            [Injectable]
            public sealed class Consumer
            {
                [Inject(ServiceLifetime.Scoped)]
                public required IContract Contract { get; init; }
            }

            public sealed class ContractImplementation : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddScoped<global::IndirectLifetime.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ThreadIsolation_lifetime_generates_thread_local_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ThreadIsolationCase;

            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton, ThreadIsolation = ThreadIsolationPolicy.Scoped)]
            public sealed class Service : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddKeyedScoped<ThreadLocal<global::ThreadIsolationCase.IContract>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "serviceProvider.GetRequiredKeyedService<ThreadLocal<global::ThreadIsolationCase.IContract>>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Indirect_closed_generic_inference_registers_constructed_implementation()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ClosedGenericInference;

            public interface IRepository<T>
            {
            }

            [Injectable]
            public sealed class UsesRepository
            {
                [Inject]
                public required IRepository<Order> Repository { get; init; }
            }

            public sealed class Repository<T> : IRepository<T>
            {
            }

            public sealed class Order
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::ClosedGenericInference.IRepository<global::ClosedGenericInference.Order>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "new global::ClosedGenericInference.Repository<global::ClosedGenericInference.Order>()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_generates_IOptions_registration_from_required_path()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("Features:MyOption")]
            public sealed class MyOption
            {
                public string? Value { get; init; }
            }

            [Injectable]
            public sealed class UsesOptions
            {
                [Inject]
                public required IOptions<MyOption> Options { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "ConfigurationBinder.Get<global::OptionsCase.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "GetSection(\"Features:MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void InjectableFactory_static_method_is_registered()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace FactoryCase;

            public interface IContract
            {
            }

            public sealed class Contract : IContract
            {
            }

            [InjectableModule("factory-module")]
            public static class ServiceFactories
            {
                [InjectableFactory<IContract>(ServiceLifetime.Singleton)]
                public static IContract Create() => new Contract();
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddSingleton<global::FactoryCase.IContract>(static serviceProvider => global::FactoryCase.ServiceFactories.Create())",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "IsModuleEnabled(modules, \"factory-module\")",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Module_grouping_generates_module_filtered_overload()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ModuleCase;

            public interface IContract
            {
            }

            [Injectable<IContract>(Module = "Billing")]
            public sealed class Service : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "public static IServiceCollection AddGenDIServices(this IServiceCollection services, params string[] modules)",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("IsModuleEnabled(modules, \"Billing\")", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void InjectableFactory_typeof_overload_with_parameters_generates_expected_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace FactoryTypeofCase;

            public interface IDependency
            {
            }

            public interface IKeyedDependency
            {
            }

            public interface IContract
            {
            }

            public sealed class Contract : IContract
            {
                public Contract(IDependency dependency, IKeyedDependency keyedDependency)
                {
                }
            }

            public static class Factories
            {
            #pragma warning disable CS0619
                [InjectableFactory(typeof(IContract), ServiceLifetime.Scoped, Group = 2, Order = 3, Key = "factory-key", ThreadIsolation = ThreadIsolationPolicy.Transient, Module = "Factories")]
            #pragma warning restore CS0619
                public static IContract Create(
                    IDependency dependency,
                    [FromKeyedServices("dep-key")] IKeyedDependency keyedDependency) =>
                    new Contract(dependency, keyedDependency);
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.Add",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "global::FactoryTypeofCase.Factories.Create(",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "\"factory-key\"",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "IsModuleEnabled(modules, \"Factories\")",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Indirect_closed_generic_inference_from_base_contract_constructs_implementation()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace BaseInferenceCase;

            public abstract class GenericBase<T>
            {
            }

            public sealed class GenericImpl<T> : GenericBase<T>
            {
            }

            [Injectable]
            public sealed class Consumer
            {
                [Inject]
                public required GenericBase<int> Service { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::BaseInferenceCase.GenericBase<int>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "new global::BaseInferenceCase.GenericImpl<int>()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Duplicate_registration_paths_are_deduplicated_for_same_contract_and_implementation()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace DuplicateCase;

            [ServiceInjection]
            public interface IContract
            {
            }

            [Injectable<IContract>]
            public sealed class Impl : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var registrationLine = "services.AddTransient<global::DuplicateCase.IContract>";
        Assert.Equal(
            1,
            generatedSource.Split(registrationLine, StringSplitOptions.None).Length - 1
        );
    }

    [Fact]
    public void Open_generic_injectable_factory_is_bypassed_with_warning()
    {
        GeneratorTestHelper.AssertNoSourceGenerated(
            """
            namespace OpenFactoryCase;

            public interface IGenericContract<T>
            {
            }

            public static class FactoryModule
            {
                [InjectableFactory]
                public static IGenericContract<T> Create<T>() => throw new System.NotImplementedException();
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
            """
            namespace OpenFactoryCase;

            public interface IGenericContract<T>
            {
            }

            public static class FactoryModule
            {
                [InjectableFactory]
                public static IGenericContract<T> Create<T>() => throw new System.NotImplementedException();
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            diagnostics,
            static diagnostic =>
                diagnostic.Id == "GENDISG001"
                && diagnostic.Severity == DiagnosticSeverity.Warning
                && diagnostic.GetMessage().Contains("InjectableFactory registration", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Open_generic_inject_contract_is_bypassed_with_warning()
    {
        GeneratorTestHelper.AssertNoSourceGenerated(
            """
            namespace OpenInjectCase;

            public interface IGenericContract<T>
            {
            }

            [Injectable]
            public sealed class Consumer<T>
            {
                [Inject]
                public required IGenericContract<T> Contract { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
            """
            namespace OpenInjectCase;

            public interface IGenericContract<T>
            {
            }

            [Injectable]
            public sealed class Consumer<T>
            {
                [Inject]
                public required IGenericContract<T> Contract { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            diagnostics,
            static diagnostic =>
                diagnostic.Id == "GENDISG001"
                && diagnostic.Severity == DiagnosticSeverity.Warning
                && diagnostic.GetMessage().Contains("Injectable class registration", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Open_generic_inferred_decorator_contract_is_bypassed_with_warning()
    {
        GeneratorTestHelper.AssertNoSourceGenerated(
            """
            namespace OpenDecoratorCase;

            [ServiceInjection]
            public interface IContract<T>
            {
            }

            [DecoratorFor]
            public sealed class OpenDecorator<T>(IContract<T> inner) : IContract<T>
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
            """
            namespace OpenDecoratorCase;

            [ServiceInjection]
            public interface IContract<T>
            {
            }

            [DecoratorFor]
            public sealed class OpenDecorator<T>(IContract<T> inner) : IContract<T>
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            diagnostics,
            static diagnostic =>
                diagnostic.Id == "GENDISG001"
                && diagnostic.Severity == DiagnosticSeverity.Warning
                && diagnostic.GetMessage().Contains("Decorator target contract discovery", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Open_generic_explicit_decorator_contract_is_bypassed_with_warning()
    {
        const string source =
            """
            namespace OpenExplicitDecoratorCase;

            public interface IContract<T>
            {
            }

            [DecoratorFor<IContract<T>>]
            public sealed class OpenDecorator<T>(IContract<T> inner) : IContract<T>
            {
            }
            """;

        GeneratorTestHelper.AssertNoSourceGenerated(
            source,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var diagnostics = GeneratorTestHelper.GetGeneratorDiagnostics(
            source,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            diagnostics,
            static diagnostic =>
                diagnostic.Id == "GENDISG001"
                && diagnostic.Severity == DiagnosticSeverity.Warning
                && diagnostic.GetMessage().Contains("Decorator target contract discovery", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Non_generic_decorator_infers_closed_service_injection_base_contract()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace BaseDecoratorContractCase;

            [ServiceInjection]
            public abstract class ContractBase
            {
            }

            [Injectable(ServiceLifetime.Singleton)]
            public sealed class ConcreteContract : ContractBase
            {
            }

            [DecoratorFor]
            public sealed class LoggingDecorator(ContractBase inner) : ContractBase
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "new global::BaseDecoratorContractCase.LoggingDecorator((new global::BaseDecoratorContractCase.ConcreteContract()))",
            generatedSource,
            StringComparison.Ordinal
        );
    }
}

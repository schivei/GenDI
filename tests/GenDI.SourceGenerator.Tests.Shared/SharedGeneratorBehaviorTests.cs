using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace GenDI.SourceGenerator.Tests;

public class SharedGeneratorBehaviorTests
{
    private const string DecoratorTargetDiscoveryWarning = "Decorator target contract discovery";

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
    public void Generated_extension_ignores_referenced_assembly_coverage_attribute()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            [Injectable]
            public sealed class ConsumerService
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedCoverageLibrary",
                """
                [assembly: GenDI.GenDICoveration(false)]

                namespace ReferencedCoverageLibrary.DependencyInjection;

                public static class GenDIServiceCollectionExtensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                        params string[] modules
                    ) => services;
                }
                """
            )
        );

        Assert.Contains(
            "global::ReferencedCoverageLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            generatedSource,
            StringComparison.Ordinal
        );

        if (TestSettings.IncludeGeneratedCodeInCoverageAttribute is false)
        {
            Assert.Contains("[ExcludeFromCodeCoverage]", generatedSource, StringComparison.Ordinal);
            return;
        }

        Assert.DoesNotContain(
            "[ExcludeFromCodeCoverage]",
            generatedSource,
            StringComparison.Ordinal
        );
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
    public void Standard_registration_with_module_and_environment_keeps_both_guards()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConditionalModuleStandard;

            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton, Module = "Billing")]
            [ConditionalInjectable("Development")]
            public sealed class Service : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddSingleton<global::ConditionalModuleStandard.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "IsModuleEnabled(modules, \"Billing\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Environment.GetEnvironmentVariable(\"DOTNET_ENVIRONMENT\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("\"Development\"", generatedSource, StringComparison.Ordinal);
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
            generatedSource
                .Split("new global::DecoratorMulti.LoggingDecorator(")
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
    public void Indirect_inject_duplicate_requests_are_deduplicated_for_single_and_multiple_paths()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace IndirectDuplicatePaths;

            public interface ISingleContract
            {
            }

            public interface IMultipleContract
            {
            }

            [Injectable]
            public sealed class FirstConsumer
            {
                [Inject]
                public required ISingleContract SingleContract { get; init; }

                [Inject(RegistrationMultiplicity = RegistrationMultiplicity.Multiple)]
                public required IMultipleContract MultipleContract { get; init; }
            }

            [Injectable]
            public sealed class SecondConsumer
            {
                [Inject]
                public required ISingleContract SingleContract { get; init; }

                [Inject(RegistrationMultiplicity = RegistrationMultiplicity.Multiple)]
                public required IMultipleContract MultipleContract { get; init; }
            }

            public sealed class SingleImplementation : ISingleContract
            {
            }

            public sealed class MultipleImplementation : IMultipleContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Equal(
            1,
            generatedSource
                .Split(
                    "services.AddTransient<global::IndirectDuplicatePaths.ISingleContract>"
                )
                .Length - 1
        );
        Assert.Equal(
            1,
            generatedSource
                .Split(
                    "services.AddTransient<global::IndirectDuplicatePaths.IMultipleContract>"
                )
                .Length - 1
        );
    }

    [Fact]
    public void Indirect_inject_supports_concrete_contracts()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace IndirectConcreteContract;

            [Injectable]
            public sealed class ConcreteDependency
            {
            }

            [Injectable]
            public sealed class Consumer
            {
                [Inject]
                public required ConcreteDependency Dependency { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::IndirectConcreteContract.ConcreteDependency>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Inject_invalid_binary_enum_overrides_fall_back_to_default_behavior()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace InvalidInjectOverrideCase;

            public interface IContract
            {
            }

            [Injectable]
            public sealed class Consumer
            {
                [Inject(
                    RegistrationMultiplicity = (RegistrationMultiplicity)42,
                    RegistrationEmission = (RegistrationEmissionStrategy)42
                )]
                public required IContract Contract { get; init; }
            }

            public sealed class ContractImplementation : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::InvalidInjectOverrideCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "services.TryAddTransient<global::InvalidInjectOverrideCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "if (!HasServiceImplementation(",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ServiceInjection_without_explicit_lifetime_defaults_to_transient()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ServiceInjectionDefaultLifetime;

            [ServiceInjection]
            public interface IContract
            {
            }

            [Injectable<IContract>]
            public sealed class ContractImplementation : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::ServiceInjectionDefaultLifetime.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Inferred_service_injection_without_explicit_lifetime_defaults_to_transient()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace InferredServiceInjectionDefaultLifetime;

            [ServiceInjection]
            public interface IContract
            {
            }

            [Injectable]
            public sealed class ContractImplementation : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::InferredServiceInjectionDefaultLifetime.IContract>",
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
    public void ThreadIsolation_registration_with_module_and_environment_keeps_both_guards()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ThreadIsolationConditionalModuleCase;

            public interface IContract
            {
            }

            [Injectable<IContract>(ServiceLifetime.Singleton, ThreadIsolation = ThreadIsolationPolicy.Scoped, Module = "Billing")]
            [ConditionalInjectable("Development")]
            public sealed class Service : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddKeyedScoped<ThreadLocal<global::ThreadIsolationConditionalModuleCase.IContract>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "serviceProvider.GetRequiredKeyedService<ThreadLocal<global::ThreadIsolationConditionalModuleCase.IContract>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "IsModuleEnabled(modules, \"Billing\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Environment.GetEnvironmentVariable(\"DOTNET_ENVIRONMENT\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("\"Development\"", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ThreadIsolation_none_named_argument_does_not_generate_thread_local_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ThreadIsolationNoneCase;

            public interface IContract
            {
            }

            [Injectable<IContract>(ThreadIsolation = ThreadIsolationPolicy.None)]
            public sealed class Service : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::ThreadIsolationNoneCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ThreadLocal<global::ThreadIsolationNoneCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Indirect_open_generic_implementation_is_not_registered()
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

        Assert.DoesNotContain(
            "services.AddTransient<global::ClosedGenericInference.IRepository<global::ClosedGenericInference.Order>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "new global::ClosedGenericInference.Repository<global::ClosedGenericInference.Order>()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Private_nested_injectable_type_is_ignored()
    {
        GeneratorTestHelper.AssertNoSourceGenerated(
            """
            namespace PrivateNestedCase;

            public sealed class Outer
            {
                [Injectable]
                private sealed class HiddenService
                {
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );
    }

    [Fact]
    public void Referenced_internal_explicit_service_contract_is_ignored()
    {
        GeneratorTestHelper.AssertNoSourceGenerated(
            string.Empty,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedContracts",
                """
                namespace ReferencedContracts;

                internal interface IHiddenContract
                {
                }

                [Injectable<IHiddenContract>(ServiceLifetime.Singleton)]
                public sealed class PublicService
                {
                }
                """
            )
        );
    }

    [Fact]
    public void Referenced_internal_implementation_is_not_used_for_indirect_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConsumerContracts;

            [Injectable]
            public sealed class Consumer
            {
                [Inject]
                public required ReferencedImplementations.IPublicContract Contract { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedImplementations",
                """
                namespace ReferencedImplementations;

                public interface IPublicContract
                {
                }

                [Injectable<IPublicContract>(ServiceLifetime.Singleton)]
                internal sealed class HiddenImplementation : IPublicContract
                {
                }
                """
            )
        );

        Assert.DoesNotContain(
            "services.AddSingleton<global::ReferencedImplementations.IPublicContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Inaccessible_base_service_injection_contract_is_ignored()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace InaccessibleBaseContractCase;

            public sealed class Container
            {
                [ServiceInjection]
                private abstract class HiddenContract
                {
                }

                [Injectable]
                public sealed class Implementation : HiddenContract
                {
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain(
            "HiddenContract",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddTransient<global::InaccessibleBaseContractCase.Container.Implementation>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Referenced_generated_extension_is_chained_instead_of_re_registering_dependency_services()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConsumerChainCase;

            public interface ILocalContract
            {
            }

            [Injectable<ILocalContract>(ServiceLifetime.Singleton)]
            public sealed class LocalService : ILocalContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedChainLibrary",
                """
                namespace ReferencedChainLibrary
                {
                    public interface IReferencedContract
                    {
                    }

                    [Injectable<IReferencedContract>(ServiceLifetime.Singleton)]
                    public sealed class ReferencedService : IReferencedContract
                    {
                    }
                }

                namespace ReferencedChainLibrary.DependencyInjection
                {
                    public static class GenDIServiceCollectionExtensions
                    {
                        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                            this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                        )
                        {
                            return AddGenDIServices(services, System.Array.Empty<string>());
                        }

                        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                            this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                            params string[] modules
                        )
                        {
                            return services;
                        }
                    }
                }
                """
            )
        );

        Assert.Contains(
            "global::ReferencedChainLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "services.AddSingleton<global::ReferencedChainLibrary.IReferencedContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddSingleton<global::ConsumerChainCase.ILocalContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Referenced_generated_extensions_are_emitted_in_ordinal_order()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConsumerChainCase;

            public interface ILocalContract
            {
            }

            [Injectable<ILocalContract>(ServiceLifetime.Singleton)]
            public sealed class LocalService : ILocalContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedZLibrary",
                """
                namespace ReferencedZLibrary.DependencyInjection;

                public static class GenDIServiceCollectionExtensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        return AddGenDIServices(services, System.Array.Empty<string>());
                    }

                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                        params string[] modules
                    )
                    {
                        return services;
                    }
                }
                """
            ),
            (
                "ReferencedALibrary",
                """
                namespace ReferencedALibrary.DependencyInjection;

                public static class GenDIServiceCollectionExtensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        return AddGenDIServices(services, System.Array.Empty<string>());
                    }

                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                        params string[] modules
                    )
                    {
                        return services;
                    }
                }
                """
            )
        );

        var aIndex = generatedSource.IndexOf(
            "global::ReferencedALibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            StringComparison.Ordinal
        );
        var zIndex = generatedSource.IndexOf(
            "global::ReferencedZLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            StringComparison.Ordinal
        );

        Assert.NotEqual(-1, aIndex);
        Assert.NotEqual(-1, zIndex);
        Assert.True(aIndex < zIndex);
    }

    [Fact]
    public void Explicit_manual_dependency_chain_prevents_automatic_chained_call_generation()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConsumerChainCase;

            public static class ManualChain
            {
                public static void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                {
                    global::ReferencedManualChainLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services);
                }
            }

            public interface ILocalContract
            {
            }

            [Injectable<ILocalContract>(ServiceLifetime.Singleton)]
            public sealed class LocalService : ILocalContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedManualChainLibrary",
                """
                namespace ReferencedManualChainLibrary.DependencyInjection;

                public static class GenDIServiceCollectionExtensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        return AddGenDIServices(services, System.Array.Empty<string>());
                    }

                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                        params string[] modules
                    )
                    {
                        return services;
                    }
                }
                """
            )
        );

        Assert.DoesNotContain(
            "global::ReferencedManualChainLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddSingleton<global::ConsumerChainCase.ILocalContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Fully_qualified_chain_text_in_comments_or_strings_does_not_block_automatic_chaining()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConsumerChainCase;

            public static class ManualChainTextOnly
            {
                // global::ReferencedManualChainLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services);
                public const string Mention = "global::ReferencedManualChainLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services)";
            }

            public interface ILocalContract
            {
            }

            [Injectable<ILocalContract>(ServiceLifetime.Singleton)]
            public sealed class LocalService : ILocalContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedManualChainLibrary",
                """
                namespace ReferencedManualChainLibrary.DependencyInjection;

                public static class GenDIServiceCollectionExtensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        return AddGenDIServices(services, System.Array.Empty<string>());
                    }

                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                        params string[] modules
                    )
                    {
                        return services;
                    }
                }
                """
            )
        );

        Assert.Contains(
            "services.AddSingleton<global::ConsumerChainCase.ILocalContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Using_based_unresolved_AddGenDIServices_invocation_blocks_automatic_chaining()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            using ReferencedManualChainLibrary.DependencyInjection;

            namespace ConsumerChainCase
            {
                public static class ManualChain
                {
                    public static void Register()
                    {
                        UnknownReceiver.AddGenDIServices();
                    }
                }

                public interface ILocalContract
                {
                }

                [Injectable<ILocalContract>(ServiceLifetime.Singleton)]
                public sealed class LocalService : ILocalContract
                {
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedManualChainLibrary",
                """
                namespace ReferencedManualChainLibrary.DependencyInjection;

                public static class GenDIServiceCollectionExtensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        return AddGenDIServices(services, System.Array.Empty<string>());
                    }

                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                        params string[] modules
                    )
                    {
                        return services;
                    }
                }
                """
            )
        );

        Assert.Contains(
            "services.AddSingleton<global::ConsumerChainCase.ILocalContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Non_generated_AddGenDIServices_member_invocation_with_dependency_using_blocks_automatic_chaining()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            using ReferencedManualChainLibrary.DependencyInjection;

            namespace ConsumerChainCase
            {
                public static class OtherChain
                {
                    public static void AddGenDIServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                    {
                    }
                }

                public static class ManualChain
                {
                    public static void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                    {
                        OtherChain.AddGenDIServices(services);
                    }
                }

                public static class GenDIServiceCollectionExtensions
                {
                    public static string AddGenDIServices(string value)
                    {
                        return value;
                    }

                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        return services;
                    }
                }

                public static class LocalExtensionCaller
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection Register(
                        Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        _ = GenDIServiceCollectionExtensions.AddGenDIServices("ok");
                        return GenDIServiceCollectionExtensions.AddGenDIServices(services);
                    }
                }

                public interface ILocalContract
                {
                }

                [Injectable<ILocalContract>(ServiceLifetime.Singleton)]
                public sealed class LocalService : ILocalContract
                {
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedManualChainLibrary",
                """
                namespace ReferencedManualChainLibrary.DependencyInjection;

                public static class GenDIServiceCollectionExtensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                    )
                    {
                        return AddGenDIServices(services, System.Array.Empty<string>());
                    }

                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                        this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                        params string[] modules
                    )
                    {
                        return services;
                    }
                }
                """
            ),
            (
                "ReferencedNoGeneratedExtension",
                """
                namespace ReferencedNoGeneratedExtension;

                public sealed class Marker
                {
                }
                """
            )
        );

        Assert.Contains(
            "services.AddSingleton<global::ConsumerChainCase.ILocalContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "global::ReferencedNoGeneratedExtension.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Referenced_extension_with_invalid_signature_is_not_chained()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConsumerChainCase;

            public interface ILocalContract
            {
            }

            [Injectable<ILocalContract>(ServiceLifetime.Singleton)]
            public sealed class LocalService : ILocalContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedInvalidChainLibrary",
                """
                namespace ReferencedInvalidChainLibrary.DependencyInjection
                {
                    public static class GenDIServiceCollectionExtensions
                    {
                        public static void AddGenDIServices(int services)
                        {
                        }
                    }
                }
                """
            )
        );

        Assert.DoesNotContain(
            "global::ReferencedInvalidChainLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Using_based_capture_adds_imported_dependency_namespace()
    {
        var invocation = Assert.IsType<InvocationExpressionSyntax>(
            SyntaxFactory.ParseExpression("UnknownReceiver.AddGenDIServices()")
        );
        var importedDependencyNamespaces = new HashSet<string>(StringComparer.Ordinal)
        {
            "ReferencedManualChainLibrary",
        };
        var explicitlyChainedNamespaces = new HashSet<string>(StringComparer.Ordinal);

        var generatorAssemblyPath = Path.Combine(
            AppContext.BaseDirectory,
            "GenDI.SourceGenerator.dll"
        );
        var generatorAssembly = Assembly.LoadFrom(generatorAssemblyPath);
        var generatorType = generatorAssembly.GetType(
            "GenDI.SourceGenerator.GenDISourceGenerator",
            throwOnError: true
        )!;
        var captureUsingBasedInvocationMethod = generatorType.GetMethod(
            "TryCaptureUsingBasedExtensionInvocation",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.NotNull(captureUsingBasedInvocationMethod);
        captureUsingBasedInvocationMethod.Invoke(
            null,
            [invocation, importedDependencyNamespaces, explicitlyChainedNamespaces]
        );

        Assert.Contains("ReferencedManualChainLibrary", explicitlyChainedNamespaces);
    }

    [Fact]
    public void Referenced_decorator_duplicate_does_not_reapply_same_concrete_implementation()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ConsumerChainCase;

            [Injectable<ReferencedDecoratorLibrary.IContract>(ServiceLifetime.Singleton)]
            public sealed class LocalService : ReferencedDecoratorLibrary.IContract
            {
            }

            namespace ReferencedDecoratorLibrary
            {
                [DecoratorFor<IContract>]
                public sealed class LoggingDecorator(IContract inner) : IContract
                {
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute,
            (
                "ReferencedDecoratorLibrary",
                """
                namespace ReferencedDecoratorLibrary
                {
                    public interface IContract
                    {
                    }

                    [Injectable<IContract>(ServiceLifetime.Singleton)]
                    public sealed class BaseService : IContract
                    {
                    }

                    [DecoratorFor<IContract>]
                    public sealed class LoggingDecorator(IContract inner) : IContract
                    {
                    }
                }

                namespace ReferencedDecoratorLibrary.DependencyInjection
                {
                    public static class GenDIServiceCollectionExtensions
                    {
                        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                            this Microsoft.Extensions.DependencyInjection.IServiceCollection services
                        )
                        {
                            return AddGenDIServices(services, System.Array.Empty<string>());
                        }

                        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGenDIServices(
                            this Microsoft.Extensions.DependencyInjection.IServiceCollection services,
                            params string[] modules
                        )
                        {
                            return services;
                        }
                    }
                }
                """
            )
        );

        Assert.Contains(
            "global::ReferencedDecoratorLibrary.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "new global::ReferencedDecoratorLibrary.LoggingDecorator(",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_generates_IOptions_registration_using_explicit_section_key()
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
            "services.AddOptions<global::OptionsCase.MyOption>().BindConfiguration(\"Features:MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ConfigurationBinder.Get<global::OptionsCase.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "serviceProvider.GetRequiredService<global::Microsoft.Extensions.Configuration.IConfiguration>()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_without_explicit_key_uses_options_type_name_as_section_key()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsDefaultSectionCase;
            using Microsoft.Extensions.Options;

            [OptionConfig]
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
            "services.AddOptions<global::OptionsDefaultSectionCase.MyOption>().BindConfiguration(\"MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_with_incompatible_constructor_does_not_generate_options_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsInvalidTypeCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("Features:MyOption")]
            public sealed class MyOption
            {
                public MyOption(string value)
                {
                    Value = value;
                }

                public string Value { get; }
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

        Assert.DoesNotContain(
            "services.AddOptions<global::OptionsInvalidTypeCase.MyOption>().BindConfiguration(\"Features:MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ConfigurationBinder.Get<global::OptionsInvalidTypeCase.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_with_whitespace_key_falls_back_to_options_type_name()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsWhitespaceKeyCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("   ")]
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
            "services.AddOptions<global::OptionsWhitespaceKeyCase.MyOption>().BindConfiguration(\"MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_with_private_parameterless_constructor_does_not_generate_options_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsPrivateCtorCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("Features:MyOption")]
            public sealed class MyOption
            {
                private MyOption() { }
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

        Assert.DoesNotContain(
            "services.AddOptions<global::OptionsPrivateCtorCase.MyOption>().BindConfiguration(\"Features:MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ConfigurationBinder.Get<global::OptionsPrivateCtorCase.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_with_internal_parameterless_constructor_does_not_generate_options_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsInternalCtorCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("Features:MyOption")]
            public sealed class MyOption
            {
                internal MyOption() { }
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

        Assert.DoesNotContain(
            "services.AddOptions<global::OptionsInternalCtorCase.MyOption>().BindConfiguration(\"Features:MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ConfigurationBinder.Get<global::OptionsInternalCtorCase.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_private_nested_type_does_not_generate_options_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsPrivateTypeCase;
            using Microsoft.Extensions.Options;

            public static class Container
            {
                [OptionConfig("Features:MyOption")]
                private sealed class MyOption
                {
                    public string? Value { get; init; }
                }

                [Injectable]
                public sealed class UsesOptions
                {
                    [Inject]
                    public required IOptions<MyOption> Options { get; init; }
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain(
            "services.AddOptions<global::OptionsPrivateTypeCase.Container.MyOption>().BindConfiguration(\"Features:MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ConfigurationBinder.Get<global::OptionsPrivateTypeCase.Container.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_protected_nested_type_does_not_generate_options_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsProtectedTypeCase;
            using Microsoft.Extensions.Options;

            public class Container
            {
                [OptionConfig("Features:MyOption")]
                protected sealed class MyOption
                {
                    public MyOption() { }
                }

                [Injectable]
                public sealed class UsesOptions
                {
                    [Inject]
                    public required IOptions<MyOption> Options { get; init; }
                }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain(
            "services.AddOptions<global::OptionsProtectedTypeCase.Container.MyOption>().BindConfiguration(\"Features:MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ConfigurationBinder.Get<global::OptionsProtectedTypeCase.Container.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void OptionConfig_value_type_uses_equivalent_ioptions_binding_path()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsStructCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("Features:MyStructOption")]
            public struct MyStructOption
            {
                public string? Value { get; set; }
            }

            [Injectable]
            public sealed class UsesOptions
            {
                [Inject]
                public required IOptions<MyStructOption> Options { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain(
            "services.AddOptions<global::OptionsStructCase.MyStructOption>().BindConfiguration(\"Features:MyStructOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "ConfigurationBinder.Get<global::OptionsStructCase.MyStructOption>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("GetSection(\"Features:MyStructOption\")", generatedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionConfig_with_inject_lifetime_override_uses_equivalent_ioptions_binding_path()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsOverrideCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("Features:MyOption")]
            public sealed class MyOption
            {
                public string? Value { get; init; }
            }

            [Injectable]
            public sealed class UsesOptions
            {
                [Inject(ServiceLifetime.Scoped)]
                public required IOptions<MyOption> Options { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddScoped<global::Microsoft.Extensions.Options.IOptions<global::OptionsOverrideCase.MyOption>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "ConfigurationBinder.Get<global::OptionsOverrideCase.MyOption>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void IOptions_without_OptionConfig_attribute_does_not_generate_configuration_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsWithoutConfigCase;
            using Microsoft.Extensions.Options;

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

        Assert.DoesNotContain(
            "services.AddOptions<global::OptionsWithoutConfigCase.MyOption>().BindConfiguration(\"MyOption\")",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "ConfigurationBinder.Get<global::OptionsWithoutConfigCase.MyOption>",
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
        Assert.Contains(
            "IsModuleEnabled(modules, \"Billing\")",
            generatedSource,
            StringComparison.Ordinal
        );
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

        Assert.Contains("services.Add", generatedSource, StringComparison.Ordinal);
        Assert.Contains(
            "global::FactoryTypeofCase.Factories.Create(",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains("\"factory-key\"", generatedSource, StringComparison.Ordinal);
        Assert.Contains(
            "IsModuleEnabled(modules, \"Factories\")",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Indirect_open_generic_base_implementation_is_not_registered()
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

        Assert.DoesNotContain(
            "services.AddTransient<global::BaseInferenceCase.GenericBase<int>>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
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
            generatedSource.Split(registrationLine).Length - 1
        );
    }

    [Fact]
    public void ServiceInjection_single_tryadd_emits_single_tryadd_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ExplicitSingleTryAddCase;

            [ServiceInjection(
                RegistrationMultiplicity = RegistrationMultiplicity.Single,
                RegistrationEmission = RegistrationEmissionStrategy.TryAdd
            )]
            public interface IContract
            {
            }

            [Injectable<IContract>]
            public sealed class FirstImpl : IContract
            {
            }

            [Injectable<IContract>]
            public sealed class SecondImpl : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.TryAddTransient<global::ExplicitSingleTryAddCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Equal(
            1,
            generatedSource
                .Split(
                    "services.TryAddTransient<global::ExplicitSingleTryAddCase.IContract>"
                )
                .Length - 1
        );
    }

    [Fact]
    public void Injectable_multiple_tryadd_emits_multiple_guarded_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ExplicitMultipleTryAddCase;

            public interface IContract
            {
            }

            [Injectable<IContract>(
                RegistrationMultiplicity = RegistrationMultiplicity.Multiple,
                RegistrationEmission = RegistrationEmissionStrategy.TryAdd
            )]
            public sealed class Impl : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "if (!HasServiceImplementation(services, typeof(global::ExplicitMultipleTryAddCase.IContract), typeof(global::ExplicitMultipleTryAddCase.Impl)))",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddTransient<global::ExplicitMultipleTryAddCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Inferred_flow_without_serviceInjection_allows_multiple_tryadd_strategy()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace InferredMultipleTryAddCase;

            public interface IContract
            {
            }

            [Injectable]
            public sealed class Consumer
            {
                [Inject(
                    RegistrationMultiplicity = RegistrationMultiplicity.Multiple,
                    RegistrationEmission = RegistrationEmissionStrategy.TryAdd
                )]
                public required IContract Contract { get; init; }
            }

            public sealed class FirstImpl : IContract
            {
            }

            public sealed class SecondImpl : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "typeof(global::InferredMultipleTryAddCase.FirstImpl)",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "typeof(global::InferredMultipleTryAddCase.SecondImpl)",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "HasServiceImplementation(services, typeof(global::InferredMultipleTryAddCase.IContract)",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ServiceInjection_strategy_prevails_over_injectable_strategy_when_contract_chain_exists()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace ServiceInjectionPrecedenceCase;

            [ServiceInjection(
                RegistrationMultiplicity = RegistrationMultiplicity.Single,
                RegistrationEmission = RegistrationEmissionStrategy.TryAdd
            )]
            public interface IContract
            {
            }

            [Injectable<IContract>(
                RegistrationMultiplicity = RegistrationMultiplicity.Multiple,
                RegistrationEmission = RegistrationEmissionStrategy.Add
            )]
            public sealed class Impl : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.TryAddTransient<global::ServiceInjectionPrecedenceCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "HasServiceImplementation(services, typeof(global::ServiceInjectionPrecedenceCase.IContract)",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(
            "services.AddTransient<global::ServiceInjectionPrecedenceCase.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ServiceInjection_single_tryadd_with_key_emits_tryadd_keyed_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace KeyedSingleTryAddCase;

            [ServiceInjection(
                RegistrationMultiplicity = RegistrationMultiplicity.Single,
                RegistrationEmission = RegistrationEmissionStrategy.TryAdd
            )]
            public interface IContract
            {
            }

            [Injectable<IContract>(Key = "contract-key")]
            public sealed class Impl : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.TryAddKeyedTransient<global::KeyedSingleTryAddCase.IContract>(\"contract-key\"",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ServiceInjection_multiple_tryadd_with_key_emits_guarded_keyed_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace KeyedMultipleTryAddCase;

            [ServiceInjection(
                RegistrationMultiplicity = RegistrationMultiplicity.Multiple,
                RegistrationEmission = RegistrationEmissionStrategy.TryAdd
            )]
            public interface IContract
            {
            }

            [Injectable<IContract>(Key = "contract-key")]
            public sealed class FirstImpl : IContract
            {
            }

            [Injectable<IContract>(Key = "contract-key")]
            public sealed class SecondImpl : IContract
            {
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "if (!HasKeyedServiceImplementation(services, typeof(global::KeyedMultipleTryAddCase.IContract), \"contract-key\", typeof(global::KeyedMultipleTryAddCase.FirstImpl)))",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "services.AddKeyedTransient<global::KeyedMultipleTryAddCase.IContract>(\"contract-key\"",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ServiceRegistrationComparer_includes_strategy_module_and_direct_statement_in_equality()
    {
        var generatorAssembly = typeof(GenDI.SourceGenerator.GenDISourceGenerator).Assembly;
        var registrationType = generatorAssembly.GetType(
            "GenDI.SourceGenerator.Models.ServiceRegistration",
            throwOnError: true
        )!;
        var comparerType = generatorAssembly.GetType(
            "GenDI.SourceGenerator.Models.ServiceRegistrationComparer",
            throwOnError: true
        )!;
        var registrationConstructor = registrationType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        )[0];

        object CreateRegistration(
            bool allowMultiple,
            bool useTryAdd,
            string? moduleName,
            string? directRegistrationStatement = null
        )
        {
            return registrationConstructor.Invoke(
                [
                    "global::TestNamespace.IService",
                    "global::TestNamespace.Impl",
                    "ServiceLifetime.Transient",
                    allowMultiple,
                    useTryAdd,
                    null,
                    "new global::TestNamespace.Impl()",
                    0,
                    0,
                    null,
                    null,
                    moduleName,
                    directRegistrationStatement,
                ]
            );
        }

        var baseline = CreateRegistration(allowMultiple: true, useTryAdd: true, moduleName: "Billing");
        var same = CreateRegistration(allowMultiple: true, useTryAdd: true, moduleName: "Billing");
        var differentMultiplicity = CreateRegistration(
            allowMultiple: false,
            useTryAdd: true,
            moduleName: "Billing"
        );
        var differentEmission = CreateRegistration(
            allowMultiple: true,
            useTryAdd: false,
            moduleName: "Billing"
        );
        var differentModule = CreateRegistration(
            allowMultiple: true,
            useTryAdd: true,
            moduleName: "Orders"
        );
        var differentDirectStatement = CreateRegistration(
            allowMultiple: true,
            useTryAdd: true,
            moduleName: "Billing",
            directRegistrationStatement: "services.AddOptions<global::TestNamespace.Impl>().BindConfiguration(\"MySection\")"
        );

        var comparer = comparerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;
        var equalsMethod = comparerType.GetMethod(
            "Equals",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            [registrationType, registrationType],
            modifiers: null
        )!;
        var getHashCodeMethod = comparerType.GetMethod(
            "GetHashCode",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            [registrationType],
            modifiers: null
        )!;

        Assert.True((bool)equalsMethod.Invoke(comparer, [baseline, same])!);
        Assert.False((bool)equalsMethod.Invoke(comparer, [baseline, differentMultiplicity])!);
        Assert.False((bool)equalsMethod.Invoke(comparer, [baseline, differentEmission])!);
        Assert.False((bool)equalsMethod.Invoke(comparer, [baseline, differentModule])!);
        Assert.False((bool)equalsMethod.Invoke(comparer, [baseline, differentDirectStatement])!);

        var baselineHash = (int)getHashCodeMethod.Invoke(comparer, [baseline])!;
        var sameHash = (int)getHashCodeMethod.Invoke(comparer, [same])!;
        Assert.Equal(baselineHash, sameHash);
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
                diagnostic is { Id: "GENDISG001", Severity: DiagnosticSeverity.Warning }
                && diagnostic
                    .GetMessage()
                    .Contains("InjectableFactory registration", StringComparison.Ordinal)
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
                diagnostic is { Id: "GENDISG001", Severity: DiagnosticSeverity.Warning }
                && diagnostic
                    .GetMessage()
                    .Contains("Injectable class registration", StringComparison.Ordinal)
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
                diagnostic is { Id: "GENDISG001", Severity: DiagnosticSeverity.Warning }
                && diagnostic
                    .GetMessage()
                    .Contains(DecoratorTargetDiscoveryWarning, StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Open_generic_explicit_decorator_contract_is_bypassed_with_warning()
    {
        // The attribute target intentionally stays open through T so the generator emits GENDISG001.
        const string source = """
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
                diagnostic is { Id: "GENDISG001", Severity: DiagnosticSeverity.Warning }
                && diagnostic
                    .GetMessage()
                    .Contains(DecoratorTargetDiscoveryWarning, StringComparison.Ordinal)
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

    [Fact]
    public void Options_registration_deduplicates_same_ioptions_contract()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsDuplicateCase;
            using Microsoft.Extensions.Options;

            [OptionConfig("MySettingsSection")]
            public sealed class MySettings
            {
                public string? Value { get; init; }
            }

            [Injectable]
            public sealed class UsesOptionsA
            {
                [Inject]
                public required IOptions<MySettings> Settings { get; init; }
            }

            [Injectable]
            public sealed class UsesOptionsB
            {
                [Inject]
                public required IOptions<MySettings> Settings { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        var expectedRegistration =
            "services.AddOptions<global::OptionsDuplicateCase.MySettings>().BindConfiguration(\"MySettingsSection\")";
        Assert.Equal(
            1,
            generatedSource.Split(expectedRegistration).Length - 1
        );
    }

    [Fact]
    public void Open_generic_explicit_service_contract_is_bypassed_with_warning()
    {
        const string source = """
            namespace OpenExplicitServiceContractCase;

            public interface IContract<T>
            {
            }

            public static class Wrapper<T>
            {
                [Injectable<IContract<T>>]
                public sealed class OpenExplicitService : IContract<T>
                {
                }
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
                diagnostic is { Id: "GENDISG001", Severity: DiagnosticSeverity.Warning }
                && diagnostic
                    .GetMessage()
                    .Contains("Injectable explicit service contract", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Analysis_private_branches_are_covered_via_reflection()
    {
        const string source = """
            using GenDI;
            using Microsoft.Extensions.Options;

            [assembly: GenDICoveration]

            namespace AnalysisCoverageCase;

            [OptionConfig]
            public interface IInvalidOptions
            {
            }

            [ServiceInjection]
            public interface IOpen<T>
            {
            }

            [ServiceInjection()]
            public interface IContract
            {
            }

            [ServiceInjection(ThreadIsolation = (ThreadIsolationPolicy)0)]
            public interface IThreadContract
            {
            }

            [ServiceInjection]
            public abstract class OpenBase<T>
            {
            }

            [Injectable]
            public sealed class ClosedImpl : IContract
            {
                [Inject(Key = typeof(string))]
                public required IContract Contract { get; init; }
            }

            [Injectable]
            public sealed class GenericImpl<T> : IOpen<T>
            {
            }

            public sealed class ConcreteFromOpenBase<T> : OpenBase<T>
            {
            }

            public abstract class BaseContract
            {
            }

            public sealed class DerivedContract : BaseContract
            {
            }

            public class GenericType<T>
            {
            }

            public unsafe class PointerHolder
            {
                public int* Value;
            }

            public sealed class ArrayHolder
            {
                public int[] Values { get; set; } = [];
            }

            public static class NoFactoryAttributeModule
            {
                public static object Create() => new object();
            }

            public class NestingConsumer
            {
                private class Container
                {
                    public class Middle
                    {
                        [OptionConfig]
                        public sealed class PublicNestedOption
                        {
                            public PublicNestedOption()
                            {
                            }
                        }
                    }
                }
            }

            public class AccessibilityConsumer
            {
                private class Hidden
                {
                    public class Visible
                    {
                    }
                }
            }
            """;

        var compilation = CreateCompilationForGeneratorCoverage(source);
        var generatorType = typeof(GenDI.SourceGenerator.GenDISourceGenerator);

        var invalidOptionType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.IInvalidOptions");
        Assert.NotNull(invalidOptionType);
        var isEligibleOptionConfigType = generatorType.GetMethod(
            "IsEligibleOptionConfigType",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(isEligibleOptionConfigType);
        Assert.False((bool)isEligibleOptionConfigType.Invoke(null, [invalidOptionType])!);

        var nestingConsumer = compilation.GetTypeByMetadataName("AnalysisCoverageCase.NestingConsumer");
        Assert.NotNull(nestingConsumer);
        var nestedOptionType = nestingConsumer
            .GetTypeMembers("Container")
            .Single()
            .GetTypeMembers("Middle")
            .Single()
            .GetTypeMembers("PublicNestedOption")
            .Single();
        Assert.NotNull(nestedOptionType);
        Assert.False((bool)isEligibleOptionConfigType.Invoke(null, [nestedOptionType])!);

        var isGeneratedCoverageEnabled = generatorType.GetMethod(
            "IsGeneratedCodeCoverageEnabled",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(isGeneratedCoverageEnabled);
        Assert.True((bool)isGeneratedCoverageEnabled.Invoke(null, [compilation])!);
        Assert.True(
            (bool)
                isGeneratedCoverageEnabled.Invoke(
                    null,
                    [CreateCompilationWithoutGenDIReference("public sealed class PlainType { }")]
                )!
        );

        var escapeStringLiteral = generatorType.GetMethod(
            "EscapeStringLiteral",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(escapeStringLiteral);
        var escaped = (string)escapeStringLiteral.Invoke(null, ["A\0B"])!;
        Assert.Equal("A\\0B", escaped);

        var buildTypedConstantExpression = generatorType.GetMethod(
            "BuildTypedConstantExpression",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(buildTypedConstantExpression);
        Assert.Equal(
            "null",
            (string?)buildTypedConstantExpression.Invoke(null, [default(TypedConstant)])
        );

        var closedImplType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.ClosedImpl");
        Assert.NotNull(closedImplType);
        var injectAttribute = closedImplType
            .GetMembers("Contract")
            .OfType<IPropertySymbol>()
            .Single()
            .GetAttributes()
            .Single();
        var typeKeyArgument = injectAttribute.NamedArguments.Single(argument => argument.Key == "Key").Value;
        Assert.Null(
            (string?)buildTypedConstantExpression.Invoke(null, [typeKeyArgument])
        );

        var convertThreadIsolation = generatorType.GetMethod(
            "ConvertThreadIsolationPolicyToLifetimeExpression",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(convertThreadIsolation);
        var threadContractType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.IThreadContract");
        Assert.NotNull(threadContractType);
        var threadIsolationArgument = threadContractType
            .GetAttributes()
            .Single()
            .NamedArguments.Single(argument => argument.Key == "ThreadIsolation")
            .Value;
        Assert.Equal(
            "ServiceLifetime.Singleton",
            (string?)convertThreadIsolation.Invoke(null, [threadIsolationArgument])
        );

        var tryGetServiceInjectionLifetime = generatorType.GetMethod(
            "TryGetServiceInjectionLifetime",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(tryGetServiceInjectionLifetime);
        var contractType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.IContract");
        Assert.NotNull(contractType);
        Assert.Equal(
            "ServiceLifetime.Transient",
            (string?)tryGetServiceInjectionLifetime.Invoke(null, [contractType])
        );

        var tryGetServiceInjectionThreadIsolation = generatorType.GetMethod(
            "TryGetServiceInjectionThreadIsolationLifetime",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(tryGetServiceInjectionThreadIsolation);
        Assert.Equal(
            "ServiceLifetime.Singleton",
            (string?)tryGetServiceInjectionThreadIsolation.Invoke(
                null,
                [threadContractType]
            )
        );

        var tryBuildDecoratorResolution = generatorType.GetMethod(
            "TryBuildDecoratorResolution",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(tryBuildDecoratorResolution);
        Assert.Null(
            (string?)tryBuildDecoratorResolution.Invoke(
                null,
                [closedImplType, "global::Different.Type", "new object()"]
            )
        );

        var noFactoryAttributeModule = compilation.GetTypeByMetadataName(
            "AnalysisCoverageCase.NoFactoryAttributeModule"
        );
        Assert.NotNull(noFactoryAttributeModule);
        var methodWithoutFactoryAttribute = noFactoryAttributeModule
            .GetMembers("Create")
            .OfType<IMethodSymbol>()
            .Single();
        var tryGetInjectableFactory = generatorType.GetMethod(
            "TryGetInjectableFactoryAttribute",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(tryGetInjectableFactory);
        var metadataArguments = new object?[]
        {
            methodWithoutFactoryAttribute,
            null,
        };
        Assert.False((bool)tryGetInjectableFactory.Invoke(null, metadataArguments)!);

        var getServiceTypes = generatorType.GetMethod(
            "GetServiceTypes",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(getServiceTypes);
        var openGenericImpl = compilation.GetTypeByMetadataName("AnalysisCoverageCase.GenericImpl`1");
        var openBaseImpl = compilation.GetTypeByMetadataName("AnalysisCoverageCase.ConcreteFromOpenBase`1");
        Assert.NotNull(openGenericImpl);
        Assert.NotNull(openBaseImpl);
        var warningType = generatorType.Assembly.GetType(
            "GenDI.SourceGenerator.Models.OpenGenericBypassWarning",
            throwOnError: true
        )!;
        var warningListType = typeof(List<>).MakeGenericType(warningType);
        var warnings = Activator.CreateInstance(warningListType)!;
        _ = getServiceTypes.Invoke(
            null,
            [compilation, openGenericImpl, "global::AnalysisCoverageCase.GenericImpl`1", null, null, warnings]
        );
        _ = getServiceTypes.Invoke(
            null,
            [compilation, openBaseImpl, "global::AnalysisCoverageCase.ConcreteFromOpenBase`1", null, null, warnings]
        );
        var warningsCount = (int)warningListType.GetProperty("Count")!.GetValue(warnings)!;
        Assert.True(warningsCount >= 1);

        var findIndirectImplementationCandidates = generatorType.GetMethod(
            "FindIndirectImplementationCandidates",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(findIndirectImplementationCandidates);
        var injectableMetadataType = generatorType.Assembly.GetType(
            "GenDI.SourceGenerator.Models.InjectableMetadata",
            throwOnError: true
        )!;
        var injectableMapType = typeof(Dictionary<,>).MakeGenericType(
            typeof(INamedTypeSymbol),
            injectableMetadataType
        );
        var injectableMap = Activator.CreateInstance(injectableMapType)!;
        var indirectResult = findIndirectImplementationCandidates.Invoke(
            null,
            [
                compilation,
                contractType,
                "global::AnalysisCoverageCase.IContract",
                System.Collections.Immutable.ImmutableArray.Create(closedImplType),
                injectableMap,
                null,
                null,
            ]
        );
        Assert.NotNull(indirectResult);
        var indirectLength = (int)indirectResult
            .GetType()
            .GetProperty("Length", BindingFlags.Instance | BindingFlags.Public)!
            .GetValue(indirectResult)!;
        Assert.True(indirectLength >= 1);

        var implementsOrInherits = generatorType.GetMethod(
            "ImplementsOrInherits",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(implementsOrInherits);
        var derivedContract = compilation.GetTypeByMetadataName("AnalysisCoverageCase.DerivedContract");
        var baseContract = compilation.GetTypeByMetadataName("AnalysisCoverageCase.BaseContract");
        Assert.NotNull(derivedContract);
        Assert.NotNull(baseContract);
        Assert.True(
            (bool)implementsOrInherits.Invoke(null, [derivedContract, baseContract])!
        );

        var isTypeAccessibleFromGeneratedCode = generatorType.GetMethod(
            "IsTypeAccessibleFromGeneratedCode",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(isTypeAccessibleFromGeneratedCode);
        var genericType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.GenericType`1");
        Assert.NotNull(genericType);
        var typeParameter = genericType.TypeParameters[0];
        Assert.False(
            (bool)isTypeAccessibleFromGeneratedCode.Invoke(null, [typeParameter, compilation])!
        );
        var arrayType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.ArrayHolder")!
            .GetMembers("Values")
            .OfType<IPropertySymbol>()
            .Single()
            .Type;
        Assert.True(
            (bool)isTypeAccessibleFromGeneratedCode.Invoke(null, [arrayType, compilation])!
        );
        var pointerType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.PointerHolder")!
            .GetMembers("Value")
            .OfType<IFieldSymbol>()
            .Single()
            .Type;
        Assert.True(
            (bool)isTypeAccessibleFromGeneratedCode.Invoke(
                null,
                [pointerType, compilation]
            )!
        );
        var accessibilityConsumerType = compilation.GetTypeByMetadataName(
            "AnalysisCoverageCase.AccessibilityConsumer"
        );
        Assert.NotNull(accessibilityConsumerType);
        var hiddenVisibleType = accessibilityConsumerType
            .GetTypeMembers("Hidden")
            .Single()
            .GetTypeMembers("Visible")
            .Single();
        Assert.NotNull(hiddenVisibleType);
        Assert.False(
            (bool)isTypeAccessibleFromGeneratedCode.Invoke(
                null,
                [hiddenVisibleType, compilation]
            )!
        );

        var isDeclaredSymbolAccessibleFromGeneratedCode = generatorType.GetMethod(
            "IsDeclaredSymbolAccessibleFromGeneratedCode",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(isDeclaredSymbolAccessibleFromGeneratedCode);
        var internalSourceCompilation = CreateCompilationForGeneratorCoverage(
            """
            namespace InternalAccessibilityCase;
            internal class InternalContract
            {
            }
            """
        );
        var internalContract = internalSourceCompilation.GetTypeByMetadataName(
            "InternalAccessibilityCase.InternalContract"
        );
        Assert.NotNull(internalContract);
        Assert.True(
            (bool)isDeclaredSymbolAccessibleFromGeneratedCode.Invoke(
                null,
                [internalContract, internalSourceCompilation]
            )!
        );

        var isClosedTypeArgument = generatorType.GetMethod(
            "IsClosedTypeArgument",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(isClosedTypeArgument);
        Assert.False((bool)isClosedTypeArgument.Invoke(null, [typeParameter])!);
        var openGenericType = compilation.GetTypeByMetadataName("AnalysisCoverageCase.GenericType`1");
        Assert.NotNull(openGenericType);
        var unboundGenericType = openGenericType.ConstructUnboundGenericType();
        Assert.False((bool)isClosedTypeArgument.Invoke(null, [unboundGenericType])!);
        Assert.True(
            (bool)isClosedTypeArgument.Invoke(null, [compilation.GetSpecialType(SpecialType.System_Int32)])!
        );

        var isCandidateClassDeclaration = generatorType.GetMethod(
            "IsCandidateClassDeclaration",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(isCandidateClassDeclaration);
        var hasCandidateAttributeName = generatorType.GetMethod(
            "HasCandidateAttributeName",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        Assert.NotNull(hasCandidateAttributeName);
        var cancellationToken = TestContext.Current.CancellationToken;
        var candidateSyntaxTree = CSharpSyntaxTree.ParseText(
            """
            using GenDI;
            public sealed class CandidateFromProperty
            {
                [Inject]
                public object? Property { get; init; }
            }
            [Obsolete]
            public sealed class NonCandidateByAttribute
            {
            }
            """,
            cancellationToken: cancellationToken
        );
        var candidateRoot = candidateSyntaxTree.GetRoot(cancellationToken);
        var candidateClass = candidateRoot
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single(classDeclaration => classDeclaration.Identifier.ValueText == "CandidateFromProperty");
        var nonCandidateClass = candidateRoot
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Single(classDeclaration =>
                classDeclaration.Identifier.ValueText == "NonCandidateByAttribute"
            );
        Assert.True(
            (bool)isCandidateClassDeclaration.Invoke(null, [candidateClass])!
        );
        Assert.False(
            (bool)hasCandidateAttributeName.Invoke(
                null,
                [nonCandidateClass.AttributeLists]
            )!
        );
    }

    private static CSharpCompilation CreateCompilationForGeneratorCoverage(string userSource)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(userSource, parseOptions);
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        var references = tpa
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(InjectableAttribute).Assembly.Location));
        references.Add(
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Options.IOptions<>).Assembly.Location)
        );
        references.Add(
            MetadataReference.CreateFromFile(
                typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location
            )
        );

        return CSharpCompilation.Create(
            assemblyName: "GeneratorCoverageReflection.Tests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true
            )
        );
    }

    private static CSharpCompilation CreateCompilationWithoutGenDIReference(string userSource)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(userSource, parseOptions);
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        var genDIAssemblyPath = typeof(InjectableAttribute).Assembly.Location;
        var references = tpa
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(path => !string.Equals(path, genDIAssemblyPath, StringComparison.OrdinalIgnoreCase))
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToList();

        return CSharpCompilation.Create(
            assemblyName: "CoverageWithoutGenDIReference.Tests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}

using System;
using Xunit;

namespace GenDI.SourceGenerator.Tests;

/// <summary>
/// Covers code paths in the generator that are not exercised by the main
/// <see cref="SharedGeneratorBehaviorTests"/> — edge cases, literal types,
/// escape sequences, and defensive branches.
/// </summary>
public class GeneratorEdgeCaseTests
{
    // ─── BuildRegistrations guards ────────────────────────────────────────────

    [Fact]
    public void Abstract_class_with_Injectable_produces_no_source()
    {
        // symbol.IsAbstract == true path in BuildRegistrations
        GeneratorTestHelper.AssertNoSourceGenerated(
            """
            [Injectable]
            public abstract class AbstractService { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );
    }

    [Fact]
    public void OptionConfig_without_any_consumer_generates_direct_options_registration()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace OptionsDirectOnly;
            using Microsoft.Extensions.Options;

            [OptionConfig("Features:DirectOnly")]
            public sealed class MyOption
            {
                public string? Value { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddOptions<global::OptionsDirectOnly.MyOption>().BindConfiguration(\"Features:DirectOnly\")",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Decorator_without_existing_implementation_registers_decorator_itself()
    {
        var generatedSource = GeneratorTestHelper.GenerateSource(
            """
            namespace DecoratorNoImpl;

            [ServiceInjection]
            public interface IContract { }

            [DecoratorFor<IContract>]
            public sealed class LoggingDecorator : IContract
            {
                public LoggingDecorator() { }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddTransient<global::DecoratorNoImpl.IContract>",
            generatedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "new global::DecoratorNoImpl.LoggingDecorator()",
            generatedSource,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Injectable_from_wrong_namespace_produces_no_source()
    {
        // IsInjectableAttribute returns false when the attribute is not GenDI.InjectableAttribute
        GeneratorTestHelper.AssertNoSourceGenerated(
            """
            namespace OtherNamespace
            {
                public sealed class InjectableAttribute : System.Attribute { }
            }

            [OtherNamespace.Injectable]
            public sealed class NotReallyInjectable { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );
    }

    // ─── Duplicate service-type deduplication ─────────────────────────────────

    [Fact]
    public void Duplicate_service_type_from_explicit_and_interface_is_deduplicated()
    {
        // GetServiceTypes produces two identical entries → Distinct() removes the dup
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace Dedup;

            [ServiceInjection]
            public interface IDupContract { }

            [Injectable<IDupContract>(ServiceLifetime.Singleton)]
            public sealed class DupService : IDupContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        // The service should appear exactly once
        var count = CountOccurrences(source, "global::Dedup.IDupContract");
        Assert.Equal(1, count);
    }

    // ─── GetProjectNamespace edge cases ───────────────────────────────────────

    [Fact]
    public void Null_assembly_name_falls_back_to_Generated_namespace()
    {
        var source = GeneratorTestHelper.GenerateSourceWithAssemblyName(
            assemblyName: null,
            userSource: """
            [Injectable]
            public sealed class SimpleService { }
            """,
            includeGeneratedCodeInCoverage: TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "namespace Generated.DependencyInjection",
            source,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Assembly_name_starting_with_digit_gets_underscore_prefix()
    {
        var source = GeneratorTestHelper.GenerateSourceWithAssemblyName(
            assemblyName: "1Project.Services",
            userSource: """
            [Injectable]
            public sealed class SimpleService { }
            """,
            includeGeneratedCodeInCoverage: TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "namespace _1Project.Services.DependencyInjection",
            source,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Assembly_name_with_special_chars_replaces_them_with_underscores()
    {
        var source = GeneratorTestHelper.GenerateSourceWithAssemblyName(
            assemblyName: "My-App.Core",
            userSource: """
            [Injectable]
            public sealed class SimpleService { }
            """,
            includeGeneratedCodeInCoverage: TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "namespace My_App.Core.DependencyInjection",
            source,
            StringComparison.Ordinal
        );
    }

    // ─── Key literal types ────────────────────────────────────────────────────

    [Fact]
    public void Null_key_renders_as_null_literal()
    {
        // BuildTypedConstantExpression: typedConstant.IsNull path
        // Use object? key = null — compile-time null constant
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace NullKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = null)]
            public sealed class NullKeyService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains(
            "services.AddKeyedSingleton<global::NullKey.IContract>(null",
            source,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Bool_true_key_renders_correctly()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace BoolKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = true)]
            public sealed class TrueKeyService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("(true", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Bool_false_key_renders_correctly()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace BoolKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = false)]
            public sealed class FalseKeyService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("(false", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Integer_key_renders_correctly()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace IntKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = 42)]
            public sealed class IntKeyService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("(42", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Float_NaN_key_renders_as_float_NaN()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace FloatKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = float.NaN)]
            public sealed class FloatNaNKeyService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("float.NaN", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Float_PositiveInfinity_key_renders_correctly()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace FloatKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = float.PositiveInfinity)]
            public sealed class FloatPosInfService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("float.PositiveInfinity", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Float_NegativeInfinity_key_renders_correctly()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace FloatKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = float.NegativeInfinity)]
            public sealed class FloatNegInfService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("float.NegativeInfinity", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Float_regular_value_key_renders_with_F_suffix()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace FloatKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = 3.14f)]
            public sealed class FloatValueService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("3.14F", source, StringComparison.Ordinal); // e.g. AddKeyedSingleton(..., 3.14F)
    }

    [Fact]
    public void Double_PositiveInfinity_key_renders_correctly()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace DoubleKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = double.PositiveInfinity)]
            public sealed class DoublePosInfService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("double.PositiveInfinity", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Double_NegativeInfinity_key_renders_correctly()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace DoubleKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = double.NegativeInfinity)]
            public sealed class DoubleNegInfService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("double.NegativeInfinity", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Double_regular_value_key_renders_with_D_suffix()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace DoubleKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = 2.71)]
            public sealed class DoubleValueService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("2.71D", source, StringComparison.Ordinal); // e.g. AddKeyedSingleton(..., 2.71D)
    }

    [Fact]
    public void Enum_key_is_rendered_as_its_underlying_integer_value()
    {
        // Roslyn exposes enum attribute values as their underlying integer.
        // BuildTypedConstantExpression matches the `int or uint or long or ulong` arm,
        // rendering the value as a plain integer literal.
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace EnumKey;

            public enum MyKind { Alpha = 1, Beta = 2 }

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = MyKind.Alpha)]
            public sealed class EnumKeyService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("(1", source, StringComparison.Ordinal);
    }

    // ─── EscapeStringLiteral full coverage ────────────────────────────────────

    [Fact]
    public void String_key_with_all_escape_sequences_renders_escaped()
    {
        // Covers: '"', '\\', '\0', '\a', '\b', '\f', '\r', '\v', control char, regular char
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace EscapeAll;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = "\"\\0\a\b\f\r\v\u0001Z")]
            public sealed class AllEscapesService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        // Should contain escaped sequences in the generated source
        Assert.Contains("\\\"", source, StringComparison.Ordinal);
        Assert.Contains("\\\\", source, StringComparison.Ordinal);
        Assert.Contains("\\0", source, StringComparison.Ordinal);
        Assert.Contains("\\a", source, StringComparison.Ordinal);
        Assert.Contains("\\b", source, StringComparison.Ordinal);
        Assert.Contains("\\f", source, StringComparison.Ordinal);
        Assert.Contains("\\r", source, StringComparison.Ordinal);
        Assert.Contains("\\v", source, StringComparison.Ordinal);
    }

    // ─── EscapeCharLiteral coverage ───────────────────────────────────────────

    [Fact]
    public void Char_key_backslash_renders_as_escaped_char_literal()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace CharKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = '\\')]
            public sealed class BackslashCharService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        // EscapeCharLiteral('\\') → "\\\\" → rendered as '\\'  (4 chars: ' \ \ ')
        Assert.Contains("'\\\\'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Char_key_single_quote_renders_as_escaped_char_literal()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace CharKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = '\'')]
            public sealed class SingleQuoteCharService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("\\''", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Char_key_newline_renders_as_escaped_char_literal()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace CharKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = '\n')]
            public sealed class NewlineCharService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("'\\n'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Char_key_carriage_return_renders_as_escaped_char_literal()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace CharKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = '\r')]
            public sealed class CrCharService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("'\\r'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Char_key_tab_renders_as_escaped_char_literal()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace CharKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = '\t')]
            public sealed class TabCharService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("'\\t'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Char_key_regular_char_renders_without_escape()
    {
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace CharKey;

            [ServiceInjection]
            public interface IContract { }

            [Injectable<IContract>(ServiceLifetime.Singleton, Key = 'A')]
            public sealed class RegularCharService : IContract { }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("'A'", source, StringComparison.Ordinal);
    }

    // ─── IsInjectableInitProperty false cases ─────────────────────────────────

    [Fact]
    public void Static_property_with_Inject_is_not_injected()
    {
        // IsInjectableInitProperty: property.IsStatic == true → false
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace StaticProp;

            public interface IDep { }

            [Injectable]
            public sealed class ServiceWithStaticProp
            {
                [Inject]
                public static IDep? StaticDep { get; set; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain("StaticDep", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Regular_setter_property_with_Inject_is_not_injected()
    {
        // IsInjectableInitProperty: !property.SetMethod.IsInitOnly → false
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace SetterProp;

            public interface IDep { }

            [Injectable]
            public sealed class ServiceWithRegularSetter
            {
                [Inject]
                public IDep? RegularDep { get; set; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain("RegularDep", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Private_init_property_with_Inject_is_not_injected()
    {
        // IsInjectableInitProperty: accessibility is private → false
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace PrivateProp;

            public interface IDep { }

            [Injectable]
            public sealed class ServiceWithPrivateProp
            {
                [Inject]
                private IDep? PrivateDep { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain("PrivateDep", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Property_with_private_init_accessor_is_not_injected()
    {
        // IsInjectableInitProperty: SetMethod.Accessibility is private → false
        var source = GeneratorTestHelper.GenerateSource(
            """
            namespace PrivateInit;

            public interface IDep { }

            [Injectable]
            public sealed class ServiceWithPrivateInit
            {
                [Inject]
                public IDep? PrivateInitDep { get; private init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.DoesNotContain("PrivateInitDep", source, StringComparison.Ordinal);
    }

    // ─── FromKeyedServices on an injectable property ──────────────────────────

    [Fact]
    public void Inject_attribute_without_key_and_FromKeyedServices_on_property_uses_keyed_resolution()
    {
        // GetFromKeyedServicesKey called on a property (not ctor param)
        // GetInjectAttributeKey returns null → GetFromKeyedServicesKey checked on property
        var source = GeneratorTestHelper.GenerateSource(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace PropKeyed;

            public interface IDep { }

            [Injectable]
            public sealed class ServiceWithKeyedProp
            {
                [Inject]
                [FromKeyedServices("dep-key")]
                public required IDep KeyedDep { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("\"dep-key\"", source, StringComparison.Ordinal);
    }

    // ─── Optional keyed resolution ────────────────────────────────────────────

    [Fact]
    public void Nullable_keyed_property_uses_optional_keyed_resolution()
    {
        // BuildResolutionExpression: useOptionalResolution=true AND keyExpression set
        // → serviceProvider.GetKeyedService<T>(key)
        var source = GeneratorTestHelper.GenerateSource(
            """
            using Microsoft.Extensions.DependencyInjection;

            namespace OptionalKeyed;

            public interface IDep { }

            [Injectable]
            public sealed class ServiceWithOptionalKeyedProp
            {
                [Inject(Key = "optional-key")]
                public IDep? KeyedDep { get; init; }
            }
            """,
            TestSettings.IncludeGeneratedCodeInCoverageAttribute
        );

        Assert.Contains("GetKeyedService", source, StringComparison.Ordinal);
        Assert.Contains("\"optional-key\"", source, StringComparison.Ordinal);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}

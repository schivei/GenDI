using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenDI.Integration.Tests;

public class AttributeUnitTests
{
    private const int DisabledThreadIsolationValue = -1;

    // ─── InjectableAttribute ──────────────────────────────────────────────────

    [Fact]
    public void InjectableAttribute_default_constructor_sets_expected_defaults()
    {
        var attr = new InjectableAttribute();

        Assert.Equal(ServiceLifetime.Transient, attr.Lifetime);
        Assert.Null(attr.ServiceType);
        Assert.Equal(int.MaxValue, attr.Order);
        Assert.Equal(int.MaxValue, attr.Group);
        Assert.Null(attr.Key);
        Assert.Equal(ThreadIsolationPolicy.None, attr.ThreadIsolation);
        Assert.Null(attr.Module);
        Assert.Equal(int.MaxValue, InjectableAttribute.DefaultOrderingValue);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void InjectableAttribute_lifetime_ctor_stores_lifetime(ServiceLifetime lifetime)
    {
        var attr = new InjectableAttribute(lifetime);

        Assert.Equal(lifetime, attr.Lifetime);
    }

    [Fact]
    public void InjectableAttribute_mutable_properties_round_trip()
    {
        var attr = new InjectableAttribute
        {
            Order = 5,
            Group = 3,
            Key = "myKey",
            ThreadIsolation = ThreadIsolationPolicy.Scoped,
            Module = "Core",
        };

        Assert.Equal(5, attr.Order);
        Assert.Equal(3, attr.Group);
        Assert.Equal("myKey", attr.Key);
        Assert.Equal(ThreadIsolationPolicy.Scoped, attr.ThreadIsolation);
        Assert.Equal("Core", attr.Module);
    }

    // ─── InjectableAttribute<T> ───────────────────────────────────────────────

    [Fact]
    public void InjectableAttributeT_default_constructor_sets_expected_defaults()
    {
        var attr = new InjectableAttribute<IServiceContract>();

        Assert.Equal(ServiceLifetime.Transient, attr.Lifetime);
        Assert.Equal(typeof(IServiceContract), attr.ServiceType);
        Assert.Equal(InjectableAttribute.DefaultOrderingValue, attr.Order);
        Assert.Equal(InjectableAttribute.DefaultOrderingValue, attr.Group);
        Assert.Null(attr.Key);
        Assert.Equal(ThreadIsolationPolicy.None, attr.ThreadIsolation);
        Assert.Null(attr.Module);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void InjectableAttributeT_lifetime_ctor_stores_lifetime(ServiceLifetime lifetime)
    {
        var attr = new InjectableAttribute<IServiceContract>(lifetime);

        Assert.Equal(lifetime, attr.Lifetime);
    }

    [Fact]
    public void InjectableAttributeT_mutable_properties_round_trip()
    {
        var attr = new InjectableAttribute<IServiceContract>
        {
            Order = 10,
            Group = 2,
            Key = 42,
            ThreadIsolation = ThreadIsolationPolicy.Singleton,
            Module = "App",
        };

        Assert.Equal(10, attr.Order);
        Assert.Equal(2, attr.Group);
        Assert.Equal(42, attr.Key);
        Assert.Equal(ThreadIsolationPolicy.Singleton, attr.ThreadIsolation);
        Assert.Equal("App", attr.Module);
    }

    // ─── InjectAttribute ──────────────────────────────────────────────────────

    [Fact]
    public void InjectAttribute_default_key_is_null()
    {
        var attr = new InjectAttribute();

        Assert.Null(attr.Key);
    }

    [Fact]
    public void InjectAttribute_key_round_trips()
    {
        var attr = new InjectAttribute
        {
            Key = "injectionKey",
        };

        Assert.Equal("injectionKey", attr.Key);
        Assert.Equal(ServiceLifetime.Transient, attr.Lifetime);
    }

    [Fact]
    public void InjectAttribute_lifetime_ctor_stores_lifetime()
    {
        var attr = new InjectAttribute(ServiceLifetime.Scoped);

        Assert.Equal(ServiceLifetime.Scoped, attr.Lifetime);
    }

    // ─── InjectOptionalAttribute ──────────────────────────────────────────────

    [Fact]
    public void InjectOptionalAttribute_default_key_is_null()
    {
        var attr = new InjectOptionalAttribute();

        Assert.Null(attr.Key);
    }

    [Fact]
    public void InjectOptionalAttribute_key_round_trips()
    {
        var attr = new InjectOptionalAttribute { Key = "optionalKey" };

        Assert.Equal("optionalKey", attr.Key);
    }

    // ─── ConditionalInjectableAttribute ───────────────────────────────────────

    [Fact]
    public void ConditionalInjectableAttribute_ctor_stores_environment_name()
    {
        var attr = new ConditionalInjectableAttribute("Development");

        Assert.Equal("Development", attr.EnvironmentName);
    }

    // ─── ServiceInjectionAttribute ───────────────────────────────────────────

    [Fact]
    public void ServiceInjectionAttribute_can_be_instantiated()
    {
        var attr = new ServiceInjectionAttribute();

        Assert.NotNull(attr);
        Assert.Equal(ServiceLifetime.Transient, attr.Lifetime);
        Assert.Equal(ThreadIsolationPolicy.None, attr.ThreadIsolation);
    }

    [Fact]
    public void ThreadIsolationPolicy_values_map_to_expected_service_lifetimes()
    {
        Assert.Equal(DisabledThreadIsolationValue, (int)ThreadIsolationPolicy.None);
        Assert.Equal((int)ServiceLifetime.Singleton, (int)ThreadIsolationPolicy.Singleton);
        Assert.Equal((int)ServiceLifetime.Scoped, (int)ThreadIsolationPolicy.Scoped);
        Assert.Equal((int)ServiceLifetime.Transient, (int)ThreadIsolationPolicy.Transient);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void ServiceInjectionAttribute_lifetime_ctor_stores_lifetime(ServiceLifetime lifetime)
    {
        var attr = new ServiceInjectionAttribute(lifetime)
        {
            ThreadIsolation = ThreadIsolationPolicy.Singleton,
        };

        Assert.Equal(lifetime, attr.Lifetime);
        Assert.Equal(ThreadIsolationPolicy.Singleton, attr.ThreadIsolation);
    }

    [Fact]
    public void DecoratorForAttribute_type_argument_is_reflected_in_runtime_type()
    {
        var attrType = typeof(DecoratorForAttribute<IServiceContract>);

        Assert.Equal("DecoratorForAttribute`1", attrType.Name);
    }

    [Fact]
    public void DecoratorForAttribute_service_type_returns_generic_contract()
    {
        var attr = new DecoratorForAttribute<IServiceContract>();

        Assert.Equal(typeof(IServiceContract), attr.ServiceType);
    }

    [Fact]
    public void OptionConfigAttribute_ctor_stores_path()
    {
        var attr = new OptionConfigAttribute("App:Feature");

        Assert.Equal("App:Feature", attr.Path);
    }

    [Fact]
    public void InjectableModuleAttribute_ctor_stores_name()
    {
        var attr = new InjectableModuleAttribute("Billing");

        Assert.Equal("Billing", attr.Name);
    }

    [Fact]
    public void InjectableFactoryAttribute_defaults_are_transient()
    {
        var attr = new InjectableFactoryAttribute();

        Assert.Equal(ServiceLifetime.Transient, attr.Lifetime);
        Assert.Equal(ThreadIsolationPolicy.None, attr.ThreadIsolation);
        Assert.Null(attr.Module);
    }

    [Fact]
    public void InjectableFactoryAttribute_with_generic_type_parameter_stores_values()
    {
        var attr = new InjectableFactoryAttribute<IServiceContract>(ServiceLifetime.Singleton)
        {
            Group = 2,
            Order = 1,
            Key = "k1",
            ThreadIsolation = ThreadIsolationPolicy.Scoped,
            Module = "M1",
        };

        Assert.Equal(ServiceLifetime.Singleton, attr.Lifetime);
        Assert.Equal(typeof(IServiceContract), attr.ServiceType);
        Assert.Equal(2, attr.Group);
        Assert.Equal(1, attr.Order);
        Assert.Equal("k1", attr.Key);
        Assert.Equal(ThreadIsolationPolicy.Scoped, attr.ThreadIsolation);
        Assert.Equal("M1", attr.Module);
    }

    [Fact]
    public void InjectableFactoryAttribute_typeof_ctor_stores_service_type_and_lifetime()
    {
        var ctor = typeof(InjectableFactoryAttribute).GetConstructor(
            new[] { typeof(Type), typeof(ServiceLifetime) }
        );
        Assert.NotNull(ctor);

        var attr = Assert.IsType<InjectableFactoryAttribute>(
            ctor!.Invoke(new object[] { typeof(IServiceContract), ServiceLifetime.Scoped })
        );

        Assert.Equal(typeof(IServiceContract), attr.ServiceType);
        Assert.Equal(ServiceLifetime.Scoped, attr.Lifetime);
        Assert.Equal(InjectableAttribute.DefaultOrderingValue, attr.Group);
        Assert.Equal(InjectableAttribute.DefaultOrderingValue, attr.Order);
        Assert.Equal(ThreadIsolationPolicy.None, attr.ThreadIsolation);
    }

    // ─── GenDICoverationAttribute ─────────────────────────────────────────────

    [Fact]
    public void GenDICoverationAttribute_default_is_true()
    {
        var attr = new GenDICoverationAttribute();

        Assert.True(attr.IncludeGeneratedCodeInCoverage);
    }

    [Fact]
    public void GenDICoverationAttribute_explicit_true()
    {
        var attr = new GenDICoverationAttribute(true);

        Assert.True(attr.IncludeGeneratedCodeInCoverage);
    }

    [Fact]
    public void GenDICoverationAttribute_explicit_false()
    {
        var attr = new GenDICoverationAttribute(false);

        Assert.False(attr.IncludeGeneratedCodeInCoverage);
    }

    // ─── Helper types ─────────────────────────────────────────────────────────
    private interface IServiceContract { }
}

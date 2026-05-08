using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenDI.Integration.Tests;

public class AttributeUnitTests
{
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
        var attr = new InjectableAttribute { Order = 5, Group = 3, Key = "myKey" };

        Assert.Equal(5, attr.Order);
        Assert.Equal(3, attr.Group);
        Assert.Equal("myKey", attr.Key);
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
            Key = 42
        };

        Assert.Equal(10, attr.Order);
        Assert.Equal(2, attr.Group);
        Assert.Equal(42, attr.Key);
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
        var attr = new InjectAttribute { Key = "injectionKey" };

        Assert.Equal("injectionKey", attr.Key);
    }

    // ─── ServiceInjectionAttribute ───────────────────────────────────────────

    [Fact]
    public void ServiceInjectionAttribute_can_be_instantiated()
    {
        var attr = new ServiceInjectionAttribute();

        Assert.NotNull(attr);
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

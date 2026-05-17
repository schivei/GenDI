using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenDI.Testing.Tests;

public class ServiceBuilderTests
{
    [Fact]
    public void Constructor_throws_when_services_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceBuilder(null!));
    }

    [Fact]
    public void BuildServiceProvider_resolves_registered_singleton()
    {
        var builder = ServiceBuilder
            .Create()
            .AddSingleton<ITestClock>(
                new FixedClock(new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero))
            );

        using var provider = builder.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ITestClock>();

        Assert.IsType<FixedClock>(resolved);
        Assert.Equal(new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero), resolved.UtcNow);
    }

    [Fact]
    public void TryAdd_and_Replace_integrate_with_di_abstractions_helpers()
    {
        var initial = new FixedClock(new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero));
        var replacement = new FixedClock(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var builder = ServiceBuilder
            .Create()
            .TryAdd(ServiceDescriptor.Singleton<ITestClock>(initial))
            .TryAdd(
                ServiceDescriptor.Singleton<ITestClock>(new FixedClock(DateTimeOffset.MinValue))
            )
            .Replace(ServiceDescriptor.Singleton<ITestClock>(replacement));

        using var provider = builder.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ITestClock>();

        Assert.Same(replacement, resolved);
    }

    [Fact]
    public void Fluent_methods_register_expected_descriptors_and_resolve()
    {
        var addedByConfigure = false;
        var addedByGenDi = false;
        var expectedServicesReference = new ServiceCollection();

        var builder = new ServiceBuilder(expectedServicesReference)
            .ConfigureServices(services =>
            {
                addedByConfigure = true;
                services.AddSingleton(new MarkerService("configured"));
            })
            .AddGenDi(services =>
            {
                addedByGenDi = true;
                services.AddSingleton<IOtherService, OtherService>();
            })
            .AddSingleton<IParameterlessService, ParameterlessService>()
            .AddScoped<IScopedDependency, ScopedDependency>()
            .AddTransient<ITransientDependency, TransientDependency>();

        Assert.True(addedByConfigure);
        Assert.True(addedByGenDi);
        Assert.Same(expectedServicesReference, builder.Services);

        using var provider = builder.BuildServiceProvider(
            validateScopes: false,
            validateOnBuild: false
        );
        using var scope = provider.CreateScope();

        Assert.IsType<ParameterlessService>(
            scope.ServiceProvider.GetRequiredService<IParameterlessService>()
        );
        Assert.IsType<ScopedDependency>(
            scope.ServiceProvider.GetRequiredService<IScopedDependency>()
        );
        Assert.IsType<TransientDependency>(
            scope.ServiceProvider.GetRequiredService<ITransientDependency>()
        );
        Assert.IsType<OtherService>(scope.ServiceProvider.GetRequiredService<IOtherService>());
        Assert.Equal("configured", scope.ServiceProvider.GetRequiredService<MarkerService>().Value);
    }

    [Fact]
    public void AddSingleton_instance_throws_when_null()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceBuilder.Create().AddSingleton<ITestClock>(null!));
    }

    [Fact]
    public void ConfigureServices_throws_when_delegate_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceBuilder.Create().ConfigureServices(null!));
    }

    [Fact]
    public void AddGenDI_throws_when_delegate_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => ServiceBuilder.Create().AddGenDi(null!));
    }

    [Fact]
    public void Descriptor_extensions_validate_arguments()
    {
        var descriptor = ServiceDescriptor.Singleton<ITestClock>(
            new FixedClock(new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero))
        );

        Assert.Throws<ArgumentNullException>(() => ServiceBuilderDescriptorExtensions.TryAdd(null!, descriptor));
        Assert.Throws<ArgumentNullException>(() => ServiceBuilderDescriptorExtensions.TryAdd(ServiceBuilder.Create(), null!));
        Assert.Throws<ArgumentNullException>(() => ServiceBuilderDescriptorExtensions.Replace(null!, descriptor));
        Assert.Throws<ArgumentNullException>(() => ServiceBuilderDescriptorExtensions.Replace(ServiceBuilder.Create(), null!));
        Assert.Throws<ArgumentNullException>(() => ServiceBuilderDescriptorExtensions.TryAddEnumerable(null!, descriptor));
        Assert.Throws<ArgumentNullException>(() => ServiceBuilderDescriptorExtensions.TryAddEnumerable(ServiceBuilder.Create(), null!));
    }

    [Fact]
    public void TryAddEnumerable_adds_only_once_for_same_implementation()
    {
        var builder = ServiceBuilder.Create();

        var descriptor = ServiceDescriptor.Transient<ITestClock, FixedClock>();
        builder.TryAddEnumerable(descriptor).TryAddEnumerable(descriptor);

        Assert.Single(builder.Services, service =>
                service.ServiceType == typeof(ITestClock)
                    && service.ImplementationType == typeof(FixedClock)
            );
    }
}

public interface ITestClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class FixedClock(DateTimeOffset utcNow) : ITestClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

public sealed record MarkerService(string Value);

public interface IOtherService;
public sealed class OtherService : IOtherService;

public interface IScopedDependency;
public sealed class ScopedDependency : IScopedDependency;

public interface ITransientDependency;
public sealed class TransientDependency : ITransientDependency;

public interface IParameterlessService;
public sealed class ParameterlessService : IParameterlessService;

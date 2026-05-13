using System;
using GenDI;
using GenDI.Integration.Tests.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenDI.Integration.Tests;

public class RealWorldIntegrationTests
{
    private static readonly object EnvironmentLock = new();

    [Fact]
    public void Generated_and_non_generated_services_resolve_together()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger, ConsoleLogger>();
        services.AddSingleton(typeof(IRepository<>), typeof(Repository<>));
        services.AddSingleton<INonGeneratedContract, NonGeneratedContract>();
        services.AddSingleton<INonGeneratedDependsOnGenerated, NonGeneratedDependsOnGenerated>();

        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var generated = provider.GetRequiredService<IGeneratedContract>();
        var nonGenerated = provider.GetRequiredService<INonGeneratedDependsOnGenerated>();

        Assert.NotNull(generated);
        Assert.NotNull(nonGenerated);
        Assert.Same(generated, nonGenerated.GeneratedContract);
        Assert.IsType<Repository<Order>>(generated.OrderRepository);
        Assert.IsType<ConsoleLogger>(generated.Logger);
        Assert.IsType<NonGeneratedContract>(generated.NonGeneratedContract);
    }

    [Fact]
    public void Non_injectable_types_are_not_registered_by_generator()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();
        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<NotGeneratedService>());
    }

    [Fact]
    public void Generated_keyed_services_resolve_with_keyed_dependencies()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IRepository<Order>>("repo", new Repository<Order>());
        services.AddKeyedSingleton<ILogger>("logger", new ConsoleLogger());

        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredKeyedService<IKeyedGeneratedContract>("generated");

        Assert.NotNull(service);
        Assert.IsType<Repository<Order>>(service.OrderRepository);
        Assert.IsType<ConsoleLogger>(service.Logger);
    }

    [Fact]
    public void InjectOptional_allows_missing_dependency_without_throwing()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IOptionalGeneratedContract>();

        Assert.NotNull(service);
        Assert.Null(service.MissingDependency);
    }

    [Fact]
    public void ConditionalInjectable_registers_only_for_matching_environment()
    {
        lock (EnvironmentLock)
        {
            var originalDotnetEnvironment = Environment.GetEnvironmentVariable(
                "DOTNET_ENVIRONMENT"
            );
            var originalAspnetEnvironment = Environment.GetEnvironmentVariable(
                "ASPNETCORE_ENVIRONMENT"
            );

            try
            {
                Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

                var nonMatchingServices = new ServiceCollection();
                nonMatchingServices.AddGenDIServices();
                using var nonMatchingProvider = nonMatchingServices.BuildServiceProvider();

                Assert.Null(nonMatchingProvider.GetService<IConditionalGeneratedContract>());

                Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

                var matchingServices = new ServiceCollection();
                matchingServices.AddGenDIServices();
                using var matchingProvider = matchingServices.BuildServiceProvider();

                Assert.NotNull(matchingProvider.GetService<IConditionalGeneratedContract>());
            }
            finally
            {
                Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnetEnvironment);
                Environment.SetEnvironmentVariable(
                    "ASPNETCORE_ENVIRONMENT",
                    originalAspnetEnvironment
                );
            }
        }
    }
}

public sealed class Order;

[ServiceInjection]
public interface IGeneratedContract
{
    IRepository<Order> OrderRepository { get; }

    ILogger Logger { get; }

    INonGeneratedContract NonGeneratedContract { get; }
}

public interface INonGeneratedContract;

public sealed class NonGeneratedContract : INonGeneratedContract;

public interface INonGeneratedDependsOnGenerated
{
    IGeneratedContract GeneratedContract { get; }
}

public sealed class NonGeneratedDependsOnGenerated(IGeneratedContract generatedContract)
    : INonGeneratedDependsOnGenerated
{
    public IGeneratedContract GeneratedContract { get; } = generatedContract;
}

public interface IRepository<T>;

public sealed class Repository<T> : IRepository<T>;

public interface ILogger;

public sealed class ConsoleLogger : ILogger;

public interface IMissingDependency;

[ServiceInjection]
public interface IOptionalGeneratedContract
{
    IMissingDependency? MissingDependency { get; }
}

[ServiceInjection]
public interface IConditionalGeneratedContract;

[ServiceInjection]
public interface IKeyedGeneratedContract
{
    IRepository<Order> OrderRepository { get; }

    ILogger Logger { get; }
}

[Injectable<IGeneratedContract>(ServiceLifetime.Singleton)]
public sealed class GeneratedService(IRepository<Order> orderRepository) : IGeneratedContract
{
    public IRepository<Order> OrderRepository { get; } = orderRepository;

    [Inject]
    public required ILogger Logger { get; init; }

    [Inject]
    public required INonGeneratedContract NonGeneratedContract { get; init; }
}

[Injectable<IKeyedGeneratedContract>(ServiceLifetime.Singleton, Key = "generated")]
public sealed class KeyedGeneratedService(
    [FromKeyedServices("repo")] IRepository<Order> orderRepository
) : IKeyedGeneratedContract
{
    public IRepository<Order> OrderRepository { get; } = orderRepository;

    [Inject(Key = "logger")]
    public required ILogger Logger { get; init; }
}

[Injectable<IOptionalGeneratedContract>(ServiceLifetime.Singleton)]
public sealed class OptionalGeneratedService : IOptionalGeneratedContract
{
    [InjectOptional]
    public required IMissingDependency? MissingDependency { get; init; }
}

[Injectable<IConditionalGeneratedContract>(ServiceLifetime.Singleton)]
[ConditionalInjectable("Development")]
public sealed class ConditionalGeneratedService : IConditionalGeneratedContract;

public sealed class NotGeneratedService;

using GenDI;
using GenDI.Integration.Tests.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenDI.Integration.Tests;

public class RealWorldIntegrationTests
{
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

public sealed class NotGeneratedService;

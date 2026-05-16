using System;
using System.Threading;
using GenDI;
using GenDI.Integration.Tests.DependencyInjection;
using GenDI.ReferenceLibrary;
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
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", string.Empty);

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

    [Fact]
    public void Indirect_inject_registration_resolves_concrete_implementation()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var consumer = provider.GetRequiredService<IndirectConsumer>();

        Assert.NotNull(consumer);
        Assert.IsType<IndirectImplementation>(consumer.Indirect);
    }

    [Fact]
    public void DecoratorFor_wraps_registered_contract()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var decorated = provider.GetRequiredService<IDecoratedContract>();

        var outer = Assert.IsType<DecoratedContractValidator>(decorated);
        var logging = Assert.IsType<DecoratedContractLogger>(outer.Inner);
        Assert.IsType<DecoratedContractCore>(logging.Inner);
    }

    [Fact]
    public void ThreadIsolation_registration_returns_per_thread_instances()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var mainThreadInstance = provider.GetRequiredService<IThreadIsolatedContract>();
        var mainThreadInstanceAgain = provider.GetRequiredService<IThreadIsolatedContract>();
        IThreadIsolatedContract? workerThreadInstance = null;

        var workerThread = new Thread(() =>
        {
            workerThreadInstance = provider.GetRequiredService<IThreadIsolatedContract>();
        });
        workerThread.Start();
        workerThread.Join();

        Assert.Same(mainThreadInstance, mainThreadInstanceAgain);
        Assert.NotNull(workerThreadInstance);
        Assert.NotSame(mainThreadInstance, workerThreadInstance);
    }

    [Fact]
    public void Referenced_library_services_are_scanned_and_registered()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IReferencedContract>());
    }

    [Fact]
    public void InjectableFactory_static_method_registration_resolves_service()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices("Factories");
        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredService<IFactoryContract>();
        Assert.Equal("factory", resolved.Kind);
    }

    [Fact]
    public void InjectableModule_filter_registers_only_selected_modules()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices("Referenced");
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IReferencedContract>());
        Assert.Null(provider.GetService<IGeneratedContract>());
    }

    [Fact]
    public void Indirect_open_generic_resolution_is_not_registered()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IUsesGenericIndirect>()
        );
        Assert.Contains(
            nameof(IGenericRepository<Order>),
            exception.Message,
            StringComparison.Ordinal
        );
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

public interface IIndirectContract;

public sealed class IndirectImplementation : IIndirectContract;

[Injectable]
public sealed class IndirectConsumer
{
    [Inject(ServiceLifetime.Scoped)]
    public required IIndirectContract Indirect { get; init; }
}

[ServiceInjection]
public interface IDecoratedContract;

[Injectable<IDecoratedContract>(ServiceLifetime.Singleton)]
public sealed class DecoratedContractCore : IDecoratedContract;

[DecoratorFor<IDecoratedContract>(Order = 0)]
public sealed class DecoratedContractLogger(IDecoratedContract inner) : IDecoratedContract
{
    public IDecoratedContract Inner { get; } = inner;
}

[DecoratorFor(Order = 1)]
public sealed class DecoratedContractValidator : IDecoratedContract
{
    [Inject]
    public required IDecoratedContract Inner { get; init; }
}

[ServiceInjection(ThreadIsolation = ThreadIsolationPolicy.Singleton)]
public interface IThreadIsolatedContract
{
    Guid InstanceId { get; }
}

[Injectable<IThreadIsolatedContract>(ServiceLifetime.Singleton)]
public sealed class ThreadIsolatedService : IThreadIsolatedContract
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

public interface IFactoryContract
{
    string Kind { get; }
}

public sealed class FactoryContract : IFactoryContract
{
    public string Kind { get; } = "factory";
}

[InjectableModule("Factories")]
public static class FactoryModule
{
    [InjectableFactory<IFactoryContract>(ServiceLifetime.Singleton)]
    public static IFactoryContract Create() => new FactoryContract();
}

public interface IGenericRepository<T>;

public sealed class GenericRepository<T> : IGenericRepository<T>;

public interface IUsesGenericIndirect
{
    IGenericRepository<Order> Repository { get; }
}

[Injectable<IUsesGenericIndirect>(ServiceLifetime.Singleton)]
public sealed class UsesGenericIndirect : IUsesGenericIndirect
{
    [Inject]
    public required IGenericRepository<Order> Repository { get; init; }
}

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenDI.Integration.Tests.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GenDI.Integration.Tests;

public class HostedServiceIntegrationTests
{
    [Fact]
    public void Hosted_worker_is_registered_and_resolvable_as_IHostedService()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToArray();

        Assert.Contains(hostedServices, service => service is HeartbeatWorker);
    }

    [Fact]
    public void Hosted_worker_receives_injected_dependencies_through_generated_factory()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var worker = provider.GetServices<IHostedService>().OfType<HeartbeatWorker>().Single();

        Assert.NotNull(worker.Heartbeat);
        Assert.Equal("beat", worker.Heartbeat.Pulse());
    }
}

[Injectable]
public sealed class Heartbeat
{
    public string Pulse() => "beat";
}

[Hosted]
public sealed class HeartbeatWorker : BackgroundService
{
    [Inject]
    public required Heartbeat Heartbeat { get; init; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}

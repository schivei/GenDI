namespace GenDI.Phase6.WorkerValidation.App;

public sealed class Worker(ILogger<Worker> logger, IHeartbeatFormatter heartbeatFormatter)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{Message}", heartbeatFormatter.Format());
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

[ServiceInjection]
public interface IHeartbeatFormatter
{
    string Format();
}

[Injectable<IHeartbeatFormatter>(ServiceLifetime.Singleton)]
public sealed class HeartbeatFormatter : IHeartbeatFormatter
{
    [Inject]
    public required TimeProvider TimeProvider { get; init; }

    public string Format() => $"worker-heartbeat:{TimeProvider.GetUtcNow():O}";
}

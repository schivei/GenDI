namespace GenDI.Phase6.WorkerValidation.App;

[Hosted]
public sealed class Worker : BackgroundService
{
    [Inject]
    public required ILogger<Worker> Logger { get; init; }

    [Inject]
    public required IHeartbeatFormatter HeartbeatFormatter { get; init; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation("{Message}", HeartbeatFormatter.Format());
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

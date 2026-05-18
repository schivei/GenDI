using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GenDI.Benchmarks;

[OptionConfig]
public class MyConfig{public string ConfigName { get; set; } }

[ServiceInjection]
public interface IBenchmarkClock
{
    DateTimeOffset UtcNow { get; }
}

[Injectable<IBenchmarkClock>(ServiceLifetime.Singleton)]
public sealed class BenchmarkClock : IBenchmarkClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

[ServiceInjection]
public interface IBenchmarkRepository
{
    int GetCount();
}

[Injectable<IBenchmarkRepository>(ServiceLifetime.Singleton)]
public sealed class BenchmarkRepository : IBenchmarkRepository
{
    public int GetCount() => 42;
}

[ServiceInjection]
public interface IBenchmarkService
{
    string Execute();
}

/// <summary>Constructor-injection variant — used by GenDI generated and manual benchmarks.</summary>
[Injectable<IBenchmarkService>(Group = 1, Order = 1)]
public sealed class BenchmarkService(IBenchmarkClock clock, IBenchmarkRepository repository)
    : IBenchmarkService
{
    [Inject] public required IOptions<MyConfig> Options { get; init; }

    public string Execute() => $"{repository.GetCount()}@{clock.UtcNow:O}";
}

// -----------------------------------------------------------------------
// Property-injection variant — identical logic, dependencies via [Inject]
// -----------------------------------------------------------------------

[ServiceInjection]
public interface IBenchmarkServiceViaProperties
{
    string Execute();
}

[Injectable<IBenchmarkServiceViaProperties>(Group = 1, Order = 2)]
public sealed class BenchmarkServiceViaProperties : IBenchmarkServiceViaProperties
{
    [Inject]
    public required IBenchmarkClock Clock { get; init; }

    [Inject]
    public required IBenchmarkRepository Repository { get; init; }

    public string Execute() => $"{Repository.GetCount()}@{Clock.UtcNow:O}";
}

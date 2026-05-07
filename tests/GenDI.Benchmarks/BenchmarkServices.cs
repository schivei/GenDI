using Microsoft.Extensions.DependencyInjection;

namespace GenDI.Benchmarks;

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

[Injectable<IBenchmarkService>(ServiceLifetime.Transient, Group = 1, Order = 1)]
public sealed class BenchmarkService(IBenchmarkClock clock, IBenchmarkRepository repository)
    : IBenchmarkService
{
    public string Execute() => $"{repository.GetCount()}@{clock.UtcNow:O}";
}

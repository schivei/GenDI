using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

/// <summary>Constructor-injection variant — used by GenDI generated and manual benchmarks.</summary>
[Injectable<IBenchmarkService>(Group = 1, Order = 1)]
public sealed class BenchmarkService(IBenchmarkClock clock, IBenchmarkRepository repository)
    : IBenchmarkService
{
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

// -----------------------------------------------------------------------
// Decorator variant — identical logic, dependencies via properties, but decorated to measure decorator overhead
// -----------------------------------------------------------------------

[ServiceInjection]
public interface IBenchmarkServiceDecorated
{
    string Execute();
}

[Injectable<IBenchmarkServiceDecorated>(Group = 1, Order = 3)]
public sealed class BenchmarkServiceDecorated : IBenchmarkServiceDecorated
{
    [Inject]
    public required IBenchmarkClock Clock { get; init; }

    [Inject]
    public required IBenchmarkRepository Repository { get; init; }

    [Inject]
    public required IOptions<BenchmarkOptions> Options { get; init; }

    public string Execute() =>
        $"{{{Options.Value.OptionValue}}} >>> {Repository.GetCount()}@{Clock.UtcNow:O}";
}

[DecoratorFor<IBenchmarkServiceDecorated>]
public sealed class BenchmarkServiceDecorator : IBenchmarkServiceDecorated
{
    [Inject]
    public required IBenchmarkServiceDecorated Inner { get; init; }

    public string Execute() => $"[Decorated]{Inner.Execute()}";
}

[OptionConfig]
public sealed class BenchmarkOptions
{
    public string OptionValue { get; set; } = "DefaultOption";
}

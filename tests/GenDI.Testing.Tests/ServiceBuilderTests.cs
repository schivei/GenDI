using GenDI.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenDI.Testing.Tests;

public class ServiceBuilderTests
{
    [Fact]
    public void BuildServiceProvider_resolves_registered_singleton()
    {
        var builder = ServiceBuilder
            .Create()
            .AddSingleton<ITestClock>(new FixedClock(new DateTimeOffset(2026, 5, 16, 0, 0, 0, TimeSpan.Zero)));

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
            .TryAdd(ServiceDescriptor.Singleton<ITestClock>(new FixedClock(DateTimeOffset.MinValue)))
            .Replace(ServiceDescriptor.Singleton<ITestClock>(replacement));

        using var provider = builder.BuildServiceProvider();
        var resolved = provider.GetRequiredService<ITestClock>();

        Assert.Same(replacement, resolved);
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

using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using GenDI.Benchmarks.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace GenDI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 3, iterationCount: 10)]
public static class StartupRegistrationBenchmarks
{
    // ------------------------------------------------------------------
    // 1. No GenDI — manual registration of the same service set, container-driven activation
    // ------------------------------------------------------------------

    [Benchmark(Description = "Manual registration (no GenDI)")]
    public static string ManualRegistrationStartup()
    {
        var services = new ServiceCollection();
        // Register the identical service set that AddGenDIServices() produces
        // so this baseline measures registration overhead, not workload difference.
        services.AddSingleton<IBenchmarkClock, BenchmarkClock>();
        services.AddSingleton<IBenchmarkRepository, BenchmarkRepository>();
        services.AddTransient<IBenchmarkService, BenchmarkService>();
        services.AddTransient<IBenchmarkServiceViaProperties>(sp =>
            new BenchmarkServiceViaProperties
            {
                Clock = sp.GetRequiredService<IBenchmarkClock>(),
                Repository = sp.GetRequiredService<IBenchmarkRepository>(),
            }
        );

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IBenchmarkService>();
        return service.Execute();
    }

    // ------------------------------------------------------------------
    // 2. GenDI — generated factory, constructor injection style
    // ------------------------------------------------------------------

    [Benchmark(Description = "GenDI: constructor injection (generated)")]
    public static string GeneratedConstructorInjectionStartup()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IBenchmarkService>();
        return service.Execute();
    }

    // ------------------------------------------------------------------
    // 3. GenDI — generated factory, property injection style
    // ------------------------------------------------------------------

    [Benchmark(Description = "GenDI: property injection (generated)")]
    public static string GeneratedPropertyInjectionStartup()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IBenchmarkServiceViaProperties>();
        return service.Execute();
    }

    // ------------------------------------------------------------------
    // 4. Reflection scanner — kept as the "worst case" baseline
    // ------------------------------------------------------------------

    [Benchmark(Description = "Reflection registration (no GenDI, assembly scan)")]
    public static string ReflectionRegistrationStartup()
    {
        var services = new ServiceCollection();
        ReflectionRegistration.AddByReflection(
            services,
            typeof(StartupRegistrationBenchmarks).Assembly
        );

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IBenchmarkService>();
        return service.Execute();
    }
}

internal static class ReflectionRegistration
{
    #pragma warning disable S3776 // benchmark baseline keeps full reflection flow in one method for readability/comparison
    public static void AddByReflection(IServiceCollection services, Assembly assembly)
    {
        foreach (var implementationType in assembly.GetTypes().Where(IsInjectableImplementation))
        {
            var injectableAttribute = implementationType
                .GetCustomAttributes(inherit: false)
                .First(attribute => IsInjectableAttribute(attribute.GetType()));

            var lifetime = (ServiceLifetime)(
                injectableAttribute.GetType().GetProperty("Lifetime")?.GetValue(injectableAttribute)
                ?? ServiceLifetime.Transient
            );
            var explicitServiceType =
                injectableAttribute
                    .GetType()
                    .GetProperty("ServiceType")
                    ?.GetValue(injectableAttribute) as Type;

            var contracts = new HashSet<Type>();
            if (explicitServiceType is not null)
            {
                contracts.Add(explicitServiceType);
            }

            foreach (var interfaceType in implementationType.GetInterfaces().Where(HasServiceInjectionAttribute))
            {
                contracts.Add(interfaceType);
            }

            var currentBase = implementationType.BaseType;
            while (currentBase is not null && currentBase != typeof(object))
            {
                if (HasServiceInjectionAttribute(currentBase))
                {
                    contracts.Add(currentBase);
                }

                currentBase = currentBase.BaseType;
            }

            if (contracts.Count == 0)
            {
                contracts.Add(implementationType);
            }

            foreach (var contract in contracts)
            {
                services.Add(ServiceDescriptor.Describe(contract, implementationType, lifetime));
            }
        }
    }
    #pragma warning restore S3776

    private static bool IsInjectableImplementation(Type type)
    {
        return type.IsClass
            && !type.IsAbstract
            && type.GetCustomAttributes(inherit: false)
                .Any(attribute => IsInjectableAttribute(attribute.GetType()));
    }

    private static bool IsInjectableAttribute(Type attributeType)
    {
        if (attributeType.FullName == "GenDI.InjectableAttribute")
        {
            return true;
        }

        return attributeType.IsGenericType
            && attributeType.GetGenericTypeDefinition().FullName == "GenDI.InjectableAttribute`1";
    }

    private static bool HasServiceInjectionAttribute(Type type)
    {
        return type.GetCustomAttributes(inherit: false)
            .Any(attribute => attribute.GetType().FullName == "GenDI.ServiceInjectionAttribute");
    }
}

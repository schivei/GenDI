using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using GenDI.Benchmarks.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GenDI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 3, iterationCount: 10)]
#pragma warning disable CA1822 // BenchmarkDotNet requires instance benchmark methods
public class StartupRegistrationBenchmarks
{
    // ------------------------------------------------------------------
    // 1. No GenDI — manual registration of the same service set, container-driven activation
    // ------------------------------------------------------------------

    [Benchmark(Description = "Manual registration (no GenDI)")]
    public string ManualRegistrationStartup()
    {
        var services = new ServiceCollection();
        // Register the identical service set that AddGenDIServices() produces
        // so this baseline measures registration overhead, not workload difference.
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddSingleton<IBenchmarkClock, BenchmarkClock>();
        services.AddSingleton<IBenchmarkRepository, BenchmarkRepository>();
        services.AddTransient<IBenchmarkService, BenchmarkService>();
        services.AddTransient<IBenchmarkServiceViaProperties>(
            sp => new BenchmarkServiceViaProperties
            {
                Clock = sp.GetRequiredService<IBenchmarkClock>(),
                Repository = sp.GetRequiredService<IBenchmarkRepository>(),
            }
        );

        var provider = services.UseGenDI();
        var service = provider.GetRequiredService<IBenchmarkService>();
        return service.Execute();
    }

    // ------------------------------------------------------------------
    // 2. GenDI — generated factory, constructor injection style
    // ------------------------------------------------------------------

    [Benchmark(Description = "GenDI: constructor injection (generated)")]
    public string GeneratedConstructorInjectionStartup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddGenDIServices();

        var provider = services.UseGenDI();
        var service = provider.GetRequiredService<IBenchmarkService>();
        return service.Execute();
    }

    // ------------------------------------------------------------------
    // 3. GenDI — generated factory, property injection style
    // ------------------------------------------------------------------

    [Benchmark(Description = "GenDI: property injection (generated)")]
    public string GeneratedPropertyInjectionStartup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddGenDIServices();

        var provider = services.UseGenDI();
        var service = provider.GetRequiredService<IBenchmarkServiceViaProperties>();
        return service.Execute();
    }

    // ------------------------------------------------------------------
    // 4. GenDI — with decorator, to show that property injection is supported even when decorators are present (which require generated factories)
    // ------------------------------------------------------------------

    [Benchmark(Description = "GenDI: with decorator, property injection (generated)")]
    public string WithDecoratorGeneratedPropertyInjectionStartup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddGenDIServices();

        var provider = services.UseGenDI();
        var service = provider.GetRequiredService<IBenchmarkServiceDecorated>();
        return service.Execute();
    }

    // ------------------------------------------------------------------
    // 5. Reflection scanner — kept as the "worst case" baseline
    // ------------------------------------------------------------------

    [Benchmark(Description = "Reflection registration (no GenDI, assembly scan)")]
    public string ReflectionRegistrationStartup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        ReflectionRegistration.AddByReflection(
            services,
            typeof(StartupRegistrationBenchmarks).Assembly
        );

        var provider = services.UseGenDI();
        var service = provider.GetRequiredService<IBenchmarkService>();
        return service.Execute();
    }
}
#pragma warning restore CA1822

internal static class ReflectionRegistration
{
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

            foreach (
                var interfaceType in implementationType
                    .GetInterfaces()
                    .Where(HasServiceInjectionAttribute)
            )
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

    private static bool IsInjectableImplementation(Type type)
    {
        return type is { IsClass: true, IsAbstract: false }
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

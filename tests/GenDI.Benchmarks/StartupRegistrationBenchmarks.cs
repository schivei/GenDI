using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using GenDI.Benchmarks.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace GenDI.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class StartupRegistrationBenchmarks
{
    [Benchmark(Description = "Generated registration startup")]
    public string GeneratedRegistrationStartup()
    {
        var services = new ServiceCollection();
        services.AddGenDIServices();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IBenchmarkService>();
        return service.Execute();
    }

    [Benchmark(Description = "Reflection registration startup")]
    public string ReflectionRegistrationStartup()
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

            foreach (var interfaceType in implementationType.GetInterfaces())
            {
                if (HasServiceInjectionAttribute(interfaceType))
                {
                    contracts.Add(interfaceType);
                }
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

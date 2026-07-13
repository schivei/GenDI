namespace GenDI.SourceGenerator;

internal static class GenDiSourceTemplates
{
    internal const string UsingsWithoutCoverage =
        "using System;\nusing System.Linq;\nusing System.Collections.Generic;\nusing System.Threading;\nusing Microsoft.Extensions.DependencyInjection;\nusing Microsoft.Extensions.DependencyInjection.Extensions;\n";

    internal const string UsingsWithCoverage =
        "using System;\nusing System.Linq;\nusing System.Collections.Generic;\nusing System.Diagnostics.CodeAnalysis;\nusing System.Threading;\nusing Microsoft.Extensions.DependencyInjection;\nusing Microsoft.Extensions.DependencyInjection.Extensions;\n";

    internal const string ExcludeFromCodeCoverageAttribute = "[ExcludeFromCodeCoverage]";

    internal const string UnkeyedAddRegistrationTemplate =
        "        services.Add{0}<{1}>(static serviceProvider => {2});";

    internal const string KeyedAddRegistrationTemplate =
        "        services.AddKeyed{0}<{1}>({2}, static (serviceProvider, _) => {3});";

    internal const string HostedServiceRegistrationTemplate =
        "        services.AddHostedService<{0}>(static serviceProvider => {1});";

    internal const string UnkeyedTryAddRegistrationTemplate =
        "        services.TryAdd{0}<{1}>(static serviceProvider => {2});";

    internal const string KeyedTryAddRegistrationTemplate =
        "        services.TryAddKeyed{0}<{1}>({2}, static (serviceProvider, _) => {3});";

    internal const string KeyedAddDecoratorTemplate =
        "        RegisterDecorator<{0}>({1}, services, ServiceLifetime.{2}, static (serviceProvider, internalKey, originalKey) => {3});";

    internal const string UnkeyedAddDecoratorTemplate =
        "        RegisterDecorator<{0}>(null, services, ServiceLifetime.{1}, static (serviceProvider, internalKey) => {2});";

    internal const string UnkeyedTryAddMultipleGuardTemplate = """
                if (!HasServiceImplementation(services, typeof({0}), typeof({1})))
                {{
                    services.Add{2}<{0}>(static serviceProvider => {3});
                }}
        """;

    internal const string KeyedTryAddMultipleGuardTemplate = """
                if (!HasKeyedServiceImplementation(services, typeof({0}), {1}, typeof({2})))
                {{
                    services.AddKeyed{3}<{0}>({1}, static (serviceProvider, _) => {4});
                }}
        """;

    internal const string ThreadIsolationCacheTemplate =
        "        services.{0}<ThreadLocal<{1}>>({2}, static (serviceProvider, _) => new ThreadLocal<{1}>(() => {3}, trackAllValues: false));";

    internal const string ThreadIsolationUnkeyedAccessTemplate =
        "        services.{0}<{1}>(static serviceProvider => serviceProvider.GetRequiredKeyedService<ThreadLocal<{1}>>({2}).Value!);";

    internal const string ThreadIsolationKeyedAccessTemplate =
        "        services.{0}<{1}>({2}, static (serviceProvider, _) => serviceProvider.GetRequiredKeyedService<ThreadLocal<{1}>>({3}).Value!);";

    internal const string ThreadIsolationCacheKeyTemplate = "\"gendi:thread:{0}:{1}:{2}\"";

    internal const string ConditionalRegistrationTemplate = """
                if ({0})
                {{
            {1}
                }}
        """;

    internal const string ConditionalRegistrationFilterTemplate = """
                {1}(
                    string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "{0}", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "{0}", StringComparison.OrdinalIgnoreCase)
                )
        """;

    internal const string ModuleRegistrationTemplate = """
                if ({0})
                {{
            {1}
                }}
        """;

    internal const string DecoratorRegistersTemplate = """

            private static void RegisterDecorator<TService>(object? serviceKey, IServiceCollection services, ServiceLifetime lifetime, Func<IServiceProvider, object?, object> factory) =>
                RegisterDecorator<TService>(serviceKey, services, lifetime, (serviceProvider, internalKey, _) => factory(serviceProvider, internalKey));

            private static void RegisterDecorator<TService>(object? serviceKey, IServiceCollection services, ServiceLifetime lifetime, Func<IServiceProvider, object?, object?, object> factory)
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(TService) && (!d.IsKeyedService || EqualityComparer<object?>.Default.Equals(d.ServiceKey, serviceKey))).ToList();

                if (descriptors.Count == 0 && serviceKey is null)
                {
                    services.Add(new ServiceDescriptor(
                        typeof(TService),
                        serviceProvider => factory(serviceProvider, null, null),
                        lifetime
                    ));

                    return;
                }
                else if (descriptors.Count == 0)
                {
                    services.Add(new ServiceDescriptor(
                        typeof(TService),
                        serviceKey,
                        (serviceProvider, key) => factory(serviceProvider, null, key),
                        lifetime
                    ));

                    return;
                }

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);

                    var internalKey = Guid.NewGuid().ToString("N");

                    if (descriptor.IsKeyedService && descriptor.KeyedImplementationInstance is not null)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            internalKey,
                            (_, _) => descriptor.KeyedImplementationInstance,
                            descriptor.Lifetime
                        ));
                    }
                    else if (descriptor.IsKeyedService && descriptor.KeyedImplementationFactory is not null)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            internalKey,
                            descriptor.KeyedImplementationFactory,
                            descriptor.Lifetime
                        ));
                    }
                    else if (descriptor.IsKeyedService && descriptor.KeyedImplementationType is not null)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            internalKey,
                            descriptor.KeyedImplementationType,
                            descriptor.Lifetime
                        ));
                    }
                    else if (descriptor.ImplementationInstance != null)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            serviceKey: internalKey,
                            factory: (_, _) => descriptor.ImplementationInstance,
                            descriptor.Lifetime
                        ));
                    }
                    else if (descriptor.ImplementationFactory != null)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            internalKey,
                            (sp, _) => descriptor.ImplementationFactory(sp),
                            descriptor.Lifetime
                        ));
                    }
                    else if (descriptor.ImplementationType != null)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            internalKey,
                            descriptor.ImplementationType,
                            descriptor.Lifetime
                        ));
                    }

                    if (serviceKey is null)
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            serviceProvider => factory(serviceProvider, internalKey, null),
                            descriptor.Lifetime
                        ));
                    }
                    else
                    {
                        services.Add(new ServiceDescriptor(
                            descriptor.ServiceType,
                            serviceKey,
                            (serviceProvider, key) => factory(serviceProvider, internalKey, key),
                            descriptor.Lifetime
                        ));
                    }
                }
            }

        """;

    internal const string CheckModuleTemplate = """

            private static bool IsModuleEnabled(string[] modules, string module)
            {
                for (var i = 0; i < modules.Length; i++)
                {
                    if (string.Equals(modules[i], module, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

        """;

    internal const string TryAddMultipleGuardTemplate = """
            
            private static bool HasServiceImplementation(IServiceCollection services, Type serviceType, Type implementationType)
            {
                for (var i = 0; i < services.Count; i++)
                {
                    var descriptor = services[i];
                    if (
                        descriptor.IsKeyedService
                        || descriptor.ServiceType != serviceType
                        || descriptor.ImplementationType != implementationType
                    )
                    {
                        continue;
                    }

                    return true;
                }

                return false;
            }

        """;

    internal const string TryAddMultipleKeyedGuardTemplate = """
            
            private static bool HasKeyedServiceImplementation(
                IServiceCollection services,
                Type serviceType,
                object? serviceKey,
                Type implementationType
            )
            {
                for (var i = 0; i < services.Count; i++)
                {
                    var descriptor = services[i];
                    if (
                        !descriptor.IsKeyedService
                        || descriptor.ServiceType != serviceType
                        || !EqualityComparer<object?>.Default.Equals(descriptor.ServiceKey, serviceKey)
                        || descriptor.KeyedImplementationType != implementationType
                    )
                    {
                        continue;
                    }

                    return true;
                }

                return false;
            }

        """;

    internal const string FileTemplate = """
        // <auto-generated />
        #pragma warning disable CS8669
        {{USINGS}}

        namespace {{NAMESPACE}}.DependencyInjection;

        /// <summary>
        /// Extension methods for registering services discovered by GenDI.
        /// </summary>
        {{EXCLUDE_FROM_COVERAGE}}internal static class GenDIServiceCollectionExtensions
        {
            private static HashSet<IServiceCollection> s_registered = [];
            {{TRY_ADD_MULTIPLE_GUARD}}
            {{TRY_ADD_MULTIPLE_KEYED_GUARD}}
            {{CHECK_MODULE}}
            {{DECORATOR_REGISTERS}}
            /// <summary>
            /// Registers services discovered by GenDI in the specified <see cref="IServiceCollection"/>.
            /// </summary>
            /// <param name="services">The service collection to add services to.</param>
            /// <returns>The same service collection, for chaining.</returns>
            public static IServiceCollection AddGenDIServices(this IServiceCollection services)
            {
                return AddGenDIServices(services, Array.Empty<string>());
            }

            /// <summary>
            /// Registers services discovered by GenDI in the specified <see cref="IServiceCollection"/>,
            /// optionally filtering by module name.
            /// </summary>
            /// <param name="services">The service collection to add services to.</param>
            /// <param name="modules">Optional module names to include. Empty means all modules.</param>
            /// <returns>The same service collection, for chaining.</returns>
            public static IServiceCollection AddGenDIServices(this IServiceCollection services, params string[] modules)
            {
                if (s_registered.Contains(services))
                    return services;

                s_registered.Add(services);

                modules ??= Array.Empty<string>();
        {{REGISTRATIONS}}
                return services;
            }
        }

        #pragma warning restore CS8669
        """;
}

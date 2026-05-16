using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
#pragma warning disable S107 // model constructors intentionally capture all immutable registration data
    private sealed class ServiceRegistration
    {
        public ServiceRegistration(
            string serviceType,
            string implementationType,
            string lifetime,
            string? threadIsolationLifetime,
            string factoryBody,
            int order,
            int group,
            string? keyExpression,
            string? environmentName,
            string? moduleName
        )
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            ThreadIsolationLifetime = threadIsolationLifetime;
            FactoryBody = factoryBody;
            Order = order;
            Group = group;
            KeyExpression = keyExpression;
            EnvironmentName = environmentName;
            ModuleName = moduleName;
        }

        public string ServiceType { get; }

        public string ImplementationType { get; }

        public string Lifetime { get; }

        public string? ThreadIsolationLifetime { get; }

        public string FactoryBody { get; }

        public int Order { get; }

        public int Group { get; }

        public string? KeyExpression { get; }

        public string? EnvironmentName { get; }

        public string? ModuleName { get; }
    }

    private sealed class ServiceContractTarget
    {
        public ServiceContractTarget(
            string serviceType,
            string? fallbackLifetime,
            string? fallbackThreadIsolationLifetime
        )
        {
            ServiceType = serviceType;
            FallbackLifetime = fallbackLifetime;
            FallbackThreadIsolationLifetime = fallbackThreadIsolationLifetime;
        }

        public string ServiceType { get; }

        public string? FallbackLifetime { get; }

        public string? FallbackThreadIsolationLifetime { get; }
    }

    private sealed class InjectableMetadata
    {
        public InjectableMetadata(
            string lifetime,
            string? explicitServiceType,
            ITypeSymbol? explicitServiceTypeSymbol,
            bool hasOpenGenericExplicitServiceType,
            int order,
            int group,
            string? keyExpression,
            string? threadIsolationLifetime,
            string? moduleName
        )
        {
            Lifetime = lifetime;
            ExplicitServiceType = explicitServiceType;
            ExplicitServiceTypeSymbol = explicitServiceTypeSymbol;
            HasOpenGenericExplicitServiceType = hasOpenGenericExplicitServiceType;
            Order = order;
            Group = group;
            KeyExpression = keyExpression;
            ThreadIsolationLifetime = threadIsolationLifetime;
            ModuleName = moduleName;
        }

        public string Lifetime { get; }

        public string? ExplicitServiceType { get; }

        public ITypeSymbol? ExplicitServiceTypeSymbol { get; }

        public bool HasOpenGenericExplicitServiceType { get; }

        public int Order { get; }

        public int Group { get; }

        public string? KeyExpression { get; }

        public string? ThreadIsolationLifetime { get; }

        public string? ModuleName { get; }
    }

    private sealed class InjectableFactoryMetadata
    {
        public InjectableFactoryMetadata(
            string lifetime,
            string? serviceType,
            ITypeSymbol? serviceTypeSymbol,
            bool hasOpenGenericServiceType,
            int order,
            int group,
            string? keyExpression,
            string? threadIsolationLifetime,
            string? moduleName
        )
        {
            Lifetime = lifetime;
            ServiceType = serviceType;
            ServiceTypeSymbol = serviceTypeSymbol;
            HasOpenGenericServiceType = hasOpenGenericServiceType;
            Order = order;
            Group = group;
            KeyExpression = keyExpression;
            ThreadIsolationLifetime = threadIsolationLifetime;
            ModuleName = moduleName;
        }

        public string Lifetime { get; }

        public string? ServiceType { get; }

        public ITypeSymbol? ServiceTypeSymbol { get; }

        public bool HasOpenGenericServiceType { get; }

        public int Order { get; }

        public int Group { get; }

        public string? KeyExpression { get; }

        public string? ThreadIsolationLifetime { get; }

        public string? ModuleName { get; }
    }

    private sealed class InjectContractRequest
    {
        public InjectContractRequest(
            INamedTypeSymbol contractSymbol,
            string serviceType,
            string? keyExpression,
            string? lifetimeOverride,
            string? moduleName
        )
        {
            ContractSymbol = contractSymbol;
            ServiceType = serviceType;
            KeyExpression = keyExpression;
            LifetimeOverride = lifetimeOverride;
            ModuleName = moduleName;
        }

        public INamedTypeSymbol ContractSymbol { get; }

        public string ServiceType { get; }

        public string? KeyExpression { get; }

        public string? LifetimeOverride { get; }

        public string? ModuleName { get; }
    }
#pragma warning restore S107

    private sealed class OpenGenericBypassWarning
    {
        public OpenGenericBypassWarning(Location location, string context, string typeDisplay)
        {
            Location = location;
            Context = context;
            TypeDisplay = typeDisplay;
        }

        public Location Location { get; }

        public string Context { get; }

        public string TypeDisplay { get; }
    }

    private sealed class RegistrationBuildResult
    {
        public RegistrationBuildResult(
            ImmutableArray<ServiceRegistration> registrations,
            ImmutableArray<OpenGenericBypassWarning> warnings
        )
        {
            Registrations = registrations;
            Warnings = warnings;
        }

        public ImmutableArray<ServiceRegistration> Registrations { get; }

        public ImmutableArray<OpenGenericBypassWarning> Warnings { get; }
    }

    private sealed class DecoratorTarget
    {
        public DecoratorTarget(INamedTypeSymbol serviceType, string displayName, int order)
        {
            ServiceType = serviceType;
            DisplayName = displayName;
            Order = order;
        }

        public INamedTypeSymbol ServiceType { get; }

        public string DisplayName { get; }

        public int Order { get; }
    }

    private sealed class ImplementationCandidate
    {
        public ImplementationCandidate(
            INamedTypeSymbol symbol,
            string implementationType,
            string lifetime,
            string? threadIsolationLifetime,
            int order,
            int group,
            string? moduleName
        )
        {
            Symbol = symbol;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            ThreadIsolationLifetime = threadIsolationLifetime;
            Order = order;
            Group = group;
            ModuleName = moduleName;
        }

        public INamedTypeSymbol Symbol { get; }

        public string ImplementationType { get; }

        public string Lifetime { get; }

        public string? ThreadIsolationLifetime { get; }

        public int Order { get; }

        public int Group { get; }

        public string? ModuleName { get; }
    }

    private sealed class ServiceRegistrationComparer : IEqualityComparer<ServiceRegistration>
    {
        public static ServiceRegistrationComparer Instance { get; } = new();

        public bool Equals(ServiceRegistration? x, ServiceRegistration? y)
        {
            return x?.ServiceType == y?.ServiceType
                && x?.ImplementationType == y?.ImplementationType
                && x?.KeyExpression == y?.KeyExpression
                && x?.EnvironmentName == y?.EnvironmentName
                && x?.ThreadIsolationLifetime == y?.ThreadIsolationLifetime
                && x?.ModuleName == y?.ModuleName;
        }

        public int GetHashCode(ServiceRegistration obj)
        {
            unchecked
            {
                var hashCode =
                    ((obj.ServiceType?.GetHashCode() ?? 0) * 397)
                    ^ (obj.ImplementationType?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (obj.KeyExpression?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (obj.EnvironmentName?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (obj.ThreadIsolationLifetime?.GetHashCode() ?? 0);
                return (hashCode * 397) ^ (obj.ModuleName?.GetHashCode() ?? 0);
            }
        }
    }
}

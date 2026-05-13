using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
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
            string? environmentName
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
            int order,
            int group,
            string? keyExpression,
            string? threadIsolationLifetime
        )
        {
            Lifetime = lifetime;
            ExplicitServiceType = explicitServiceType;
            Order = order;
            Group = group;
            KeyExpression = keyExpression;
            ThreadIsolationLifetime = threadIsolationLifetime;
        }

        public string Lifetime { get; }

        public string? ExplicitServiceType { get; }

        public int Order { get; }

        public int Group { get; }

        public string? KeyExpression { get; }

        public string? ThreadIsolationLifetime { get; }
    }

    private sealed class InjectContractRequest
    {
        public InjectContractRequest(
            INamedTypeSymbol contractSymbol,
            string serviceType,
            string? keyExpression,
            string? lifetimeOverride
        )
        {
            ContractSymbol = contractSymbol;
            ServiceType = serviceType;
            KeyExpression = keyExpression;
            LifetimeOverride = lifetimeOverride;
        }

        public INamedTypeSymbol ContractSymbol { get; }

        public string ServiceType { get; }

        public string? KeyExpression { get; }

        public string? LifetimeOverride { get; }
    }

    private sealed class DecoratorTarget
    {
        public DecoratorTarget(INamedTypeSymbol serviceType, string displayName)
        {
            ServiceType = serviceType;
            DisplayName = displayName;
        }

        public INamedTypeSymbol ServiceType { get; }

        public string DisplayName { get; }
    }

    private sealed class ImplementationCandidate
    {
        public ImplementationCandidate(
            INamedTypeSymbol symbol,
            string implementationType,
            string lifetime,
            string? threadIsolationLifetime,
            int order,
            int group
        )
        {
            Symbol = symbol;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            ThreadIsolationLifetime = threadIsolationLifetime;
            Order = order;
            Group = group;
        }

        public INamedTypeSymbol Symbol { get; }

        public string ImplementationType { get; }

        public string Lifetime { get; }

        public string? ThreadIsolationLifetime { get; }

        public int Order { get; }

        public int Group { get; }
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
                && x?.ThreadIsolationLifetime == y?.ThreadIsolationLifetime;
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
                return (hashCode * 397) ^ (obj.ThreadIsolationLifetime?.GetHashCode() ?? 0);
            }
        }
    }
}

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
    private sealed class ServiceRegistration
    {
        public ServiceRegistration(
            string serviceType,
            string implementationType,
            string lifetime,
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
            FactoryBody = factoryBody;
            Order = order;
            Group = group;
            KeyExpression = keyExpression;
            EnvironmentName = environmentName;
        }

        public string ServiceType { get; }

        public string ImplementationType { get; }

        public string Lifetime { get; }

        public string FactoryBody { get; }

        public int Order { get; }

        public int Group { get; }

        public string? KeyExpression { get; }

        public string? EnvironmentName { get; }
    }

    private sealed class ServiceContractTarget
    {
        public ServiceContractTarget(string serviceType, string? fallbackLifetime)
        {
            ServiceType = serviceType;
            FallbackLifetime = fallbackLifetime;
        }

        public string ServiceType { get; }

        public string? FallbackLifetime { get; }
    }

    private sealed class ServiceRegistrationComparer : IEqualityComparer<ServiceRegistration>
    {
        public static ServiceRegistrationComparer Instance { get; } = new();

        public bool Equals(ServiceRegistration? x, ServiceRegistration? y)
        {
            return x?.ServiceType == y?.ServiceType
                && x?.ImplementationType == y?.ImplementationType
                && x?.KeyExpression == y?.KeyExpression
                && x?.EnvironmentName == y?.EnvironmentName;
        }

        public int GetHashCode(ServiceRegistration obj)
        {
            unchecked
            {
                var hashCode =
                    ((obj.ServiceType?.GetHashCode() ?? 0) * 397)
                    ^ (obj.ImplementationType?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (obj.KeyExpression?.GetHashCode() ?? 0);
                return (hashCode * 397) ^ (obj.EnvironmentName?.GetHashCode() ?? 0);
            }
        }
    }
}

using System.Diagnostics.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

[ExcludeFromCodeCoverage]
internal sealed class ServiceRegistrationComparer : IEqualityComparer<ServiceRegistration>
{
    public static ServiceRegistrationComparer Instance { get; } = new();

    public bool Equals(ServiceRegistration? x, ServiceRegistration? y)
    {
        return x?.ServiceType == y?.ServiceType
            && x?.ImplementationType == y?.ImplementationType
            && x?.KeyExpression == y?.KeyExpression
            && x?.Environments == y?.Environments
            && x?.ThreadIsolationLifetime == y?.ThreadIsolationLifetime
            && x?.AllowMultiple == y?.AllowMultiple
            && x?.UseTryAdd == y?.UseTryAdd
            && x?.ModuleName == y?.ModuleName
            && x?.DirectRegistrationStatement == y?.DirectRegistrationStatement;
    }

    public int GetHashCode(ServiceRegistration obj)
    {
        unchecked
        {
            var hashCode =
                obj.ServiceType.GetHashCode() * 397 ^ obj.ImplementationType.GetHashCode();
            hashCode = hashCode * 397 ^ (obj.KeyExpression?.GetHashCode() ?? 0);
            hashCode = hashCode * 397 ^ (obj.Environments?.GetHashCode() ?? 0);
            hashCode = hashCode * 397 ^ (obj.ThreadIsolationLifetime?.GetHashCode() ?? 0);
            hashCode = hashCode * 397 ^ obj.AllowMultiple.GetHashCode();
            hashCode = hashCode * 397 ^ obj.UseTryAdd.GetHashCode();
            hashCode = hashCode * 397 ^ (obj.ModuleName?.GetHashCode() ?? 0);
            return hashCode * 397 ^ (obj.DirectRegistrationStatement?.GetHashCode() ?? 0);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Marks interfaces and abstract contracts that should be considered service contracts during generation.
/// </summary>
/// <remarks>
/// When an <c>[Injectable]</c> class implements or inherits contracts annotated with this attribute,
/// GenDI registers the generated activation for each discovered contract type.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Class,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class ServiceInjectionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInjectionAttribute"/> class.
    /// </summary>
    /// <param name="lifetime">
    /// Optional fallback lifetime used when no explicit lifetime is provided by <c>[Injectable]</c>.
    /// </param>
    public ServiceInjectionAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Gets the fallback service lifetime for registrations targeting this contract.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Optional thread-isolation registration lifetime fallback.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;
}

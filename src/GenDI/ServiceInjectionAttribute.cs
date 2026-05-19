using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Marks interfaces and abstract contracts that should be considered service contracts during generation.
/// </summary>
/// <remarks>
/// When an <c>[Injectable]</c> class implements or inherits contracts annotated with this attribute,
/// GenDI registers the generated activation for each discovered contract type.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="ServiceInjectionAttribute"/> class.
/// </remarks>
/// <param name="lifetime">
/// Optional fallback lifetime used when no explicit lifetime is provided by <c>[Injectable]</c>.
/// </param>
[AttributeUsage(
    AttributeTargets.Interface | AttributeTargets.Class,
    Inherited = false,
    AllowMultiple = false
)]
public sealed class ServiceInjectionAttribute(ServiceLifetime lifetime = ServiceLifetime.Transient) : Attribute
{

    /// <summary>
    /// Gets the fallback service lifetime for registrations targeting this contract.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;

    /// <summary>
    /// Optional thread-isolation registration lifetime fallback.
    /// </summary>
    public ThreadIsolationPolicy ThreadIsolation { get; set; } = ThreadIsolationPolicy.None;

    /// <summary>
    /// Optional registration multiplicity fallback for annotated contracts.
    /// </summary>
    public RegistrationMultiplicity RegistrationMultiplicity { get; set; } =
        RegistrationMultiplicity.Multiple;

    /// <summary>
    /// Optional registration emission fallback for annotated contracts.
    /// </summary>
    public RegistrationEmissionStrategy RegistrationEmission { get; set; } =
        RegistrationEmissionStrategy.Add;
}

namespace GenDI;

/// <summary>
/// Defines whether GenDI should emit single or multiple registrations for a contract.
/// </summary>
public enum RegistrationMultiplicity
{
    /// <summary>
    /// Emits a single registration strategy for the contract.
    /// </summary>
    Single = 0,

    /// <summary>
    /// Emits a multiple-registration strategy for the contract.
    /// </summary>
    Multiple = 1,
}

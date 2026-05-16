namespace GenDI;

/// <summary>
/// Defines which API family GenDI should emit for service registrations.
/// </summary>
public enum RegistrationEmissionStrategy
{
    /// <summary>
    /// Emits <c>Add*</c> registrations.
    /// </summary>
    Add = 0,

    /// <summary>
    /// Emits <c>TryAdd*</c> registrations.
    /// </summary>
    TryAdd = 1,
}

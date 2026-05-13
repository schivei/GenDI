namespace GenDI;

/// <summary>
/// Marks a class as a decorator for the specified service contract.
/// </summary>
/// <typeparam name="TService">Service contract decorated by the target type.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class DecoratorForAttribute<TService> : Attribute
{
    /// <summary>
    /// Gets the decorated service contract type.
    /// </summary>
    public Type ServiceType => typeof(TService);
}

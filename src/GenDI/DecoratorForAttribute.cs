namespace GenDI;

/// <summary>
/// Marks a class as a decorator and lets GenDI infer the decorated service contract from the
/// implemented or inherited <c>[ServiceInjection]</c> contract.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class DecoratorForAttribute : Attribute
{
    /// <summary>
    /// Explicit service contract for non-generic usage. This always returns
    /// <see langword="null"/>; use <see cref="DecoratorForAttribute{TService}"/> when callers
    /// need an explicit contract type.
    /// </summary>
#pragma warning disable CA1822 // kept as instance member for API parity with generic variant
#pragma warning disable S2325 // kept as instance member for API parity with generic variant
    public Type? ServiceType => null;
#pragma warning restore S2325
#pragma warning restore CA1822

    /// <summary>
    /// Optional pipeline ordering value. Lower values wrap earlier and ties fall back to ordinal
    /// decorator type name ordering.
    /// </summary>
    public int Order { get; set; } = InjectableAttribute.DefaultOrderingValue;
}

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

    /// <summary>
    /// Optional pipeline ordering value. Lower values wrap earlier and ties fall back to ordinal
    /// decorator type name ordering.
    /// </summary>
    public int Order { get; set; } = InjectableAttribute.DefaultOrderingValue;
}

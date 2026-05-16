using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GenDI.Testing;

/// <summary>
/// Provides integration helpers between <see cref="ServiceBuilder"/> and DI abstractions descriptor helpers.
/// </summary>
public static class ServiceBuilderDescriptorExtensions
{
    /// <summary>
    /// Adds the descriptor only when the same service type is not already registered.
    /// </summary>
    /// <param name="builder">Target service builder.</param>
    /// <param name="descriptor">Descriptor to attempt to add.</param>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public static ServiceBuilder TryAdd(this ServiceBuilder builder, ServiceDescriptor descriptor)
    {
        ThrowIfNull(builder, nameof(builder));
        ThrowIfNull(descriptor, nameof(descriptor));
        builder.Services.TryAdd(descriptor);
        return builder;
    }

    /// <summary>
    /// Replaces the first descriptor with matching service type.
    /// </summary>
    /// <param name="builder">Target service builder.</param>
    /// <param name="descriptor">Replacement descriptor.</param>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public static ServiceBuilder Replace(this ServiceBuilder builder, ServiceDescriptor descriptor)
    {
        ThrowIfNull(builder, nameof(builder));
        ThrowIfNull(descriptor, nameof(descriptor));
        builder.Services.Replace(descriptor);
        return builder;
    }

    /// <summary>
    /// Adds the descriptor to enumerable registrations only if equivalent one is not present.
    /// </summary>
    /// <param name="builder">Target service builder.</param>
    /// <param name="descriptor">Descriptor to attempt to add.</param>
    /// <returns>The current <see cref="ServiceBuilder"/>.</returns>
    public static ServiceBuilder TryAddEnumerable(
        this ServiceBuilder builder,
        ServiceDescriptor descriptor
    )
    {
        ThrowIfNull(builder, nameof(builder));
        ThrowIfNull(descriptor, nameof(descriptor));
        builder.Services.TryAddEnumerable(descriptor);
        return builder;
    }

    private static void ThrowIfNull(object? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace GenDI;

/// <summary>
/// Controls whether generated registrations resolve through a thread-local cache.
/// </summary>
public enum ThreadIsolationPolicy
{
    /// <summary>
    /// Disables thread-local caching.
    /// </summary>
    None = -1,

    /// <summary>
    /// Enables thread-local caching with singleton registration semantics.
    /// </summary>
    Singleton = ServiceLifetime.Singleton,

    /// <summary>
    /// Enables thread-local caching with scoped registration semantics.
    /// </summary>
    Scoped = ServiceLifetime.Scoped,

    /// <summary>
    /// Enables thread-local caching with transient registration semantics.
    /// </summary>
    Transient = ServiceLifetime.Transient,
}

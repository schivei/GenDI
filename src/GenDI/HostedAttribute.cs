namespace GenDI;

/// <summary>
/// Marks a concrete class as a hosted service so the GenDI source generator emits its
/// registration through <c>services.AddHostedService&lt;TWorker&gt;(...)</c> as part of the
/// generated <c>AddGenDIServices</c> extension.
/// </summary>
/// <remarks>
/// <para>
/// The annotated type must implement <see cref="T:Microsoft.Extensions.Hosting.IHostedService"/>
/// either directly or through its base-class chain (for example, by deriving from
/// <c>Microsoft.Extensions.Hosting.BackgroundService</c>). When it does not, the generator
/// reports diagnostic <c>GENDISG002</c> and skips the type instead of emitting an
/// uncompilable registration.
/// </para>
/// <para>
/// The generated registration uses the factory overload of <c>AddHostedService</c>, so the
/// worker is created through a lambda rather than the container's constructor activation.
/// This lets the generator honor <see cref="InjectAttribute"/> init-only property injection
/// in addition to constructor injection. Dependencies are resolved from the
/// <see cref="T:System.IServiceProvider"/> at activation time and must be registered
/// separately (through other GenDI attributes or by the host, such as logging).
/// </para>
/// <para>
/// This attribute is independent of <see cref="InjectableAttribute"/>: applying it does not
/// register the type as a resolvable service, only as a hosted service.
/// </para>
/// <example>
/// The following worker is registered automatically by <c>AddGenDIServices</c>:
/// <code>
/// [Hosted]
/// internal sealed class Worker : BackgroundService
/// {
///     [Inject]
///     internal required ILogger&lt;Worker&gt; Logger { get; init; }
///
///     protected override Task ExecuteAsync(CancellationToken stoppingToken)
///     {
///         // implementation
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HostedAttribute : Attribute;

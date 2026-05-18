using System.Collections.Immutable;

namespace GenDI.SourceGenerator.Models;

internal sealed class RegistrationBuildResult(
    ImmutableArray<ServiceRegistration> registrations,
    ImmutableArray<OpenGenericBypassWarning> warnings
)
{
    public ImmutableArray<ServiceRegistration> Registrations { get; } = registrations;

    public ImmutableArray<OpenGenericBypassWarning> Warnings { get; } = warnings;
}

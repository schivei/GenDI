using System.Collections.Immutable;

namespace GenDI.SourceGenerator;

internal sealed class RegistrationBuildResult
{
    public RegistrationBuildResult(
        ImmutableArray<ServiceRegistration> registrations,
        ImmutableArray<OpenGenericBypassWarning> warnings
    )
    {
        Registrations = registrations;
        Warnings = warnings;
    }

    public ImmutableArray<ServiceRegistration> Registrations { get; }

    public ImmutableArray<OpenGenericBypassWarning> Warnings { get; }
}

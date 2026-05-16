using System.Collections.Immutable;

namespace GenDI.SourceGenerator;

internal sealed class RegistrationBuildResult
{
    public RegistrationBuildResult(
        ImmutableArray<ServiceRegistration> registrations,
        ImmutableArray<string> chainedExtensionCalls,
        ImmutableArray<OpenGenericBypassWarning> warnings
    )
    {
        Registrations = registrations;
        ChainedExtensionCalls = chainedExtensionCalls;
        Warnings = warnings;
    }

    public ImmutableArray<ServiceRegistration> Registrations { get; }

    public ImmutableArray<string> ChainedExtensionCalls { get; }

    public ImmutableArray<OpenGenericBypassWarning> Warnings { get; }
}

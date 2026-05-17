using System.Collections.Immutable;

namespace GenDI.SourceGenerator.Models;

internal sealed class RegistrationBuildResult(
    ImmutableArray<ServiceRegistration> registrations,
    ImmutableArray<string> chainedExtensionCalls,
    ImmutableArray<OpenGenericBypassWarning> warnings
)
{
    public ImmutableArray<ServiceRegistration> Registrations { get; } = registrations;

    public ImmutableArray<string> ChainedExtensionCalls { get; } = chainedExtensionCalls;

    public ImmutableArray<OpenGenericBypassWarning> Warnings { get; } = warnings;
}

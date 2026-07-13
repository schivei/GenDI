using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator.Models;

internal sealed class RegistrationBuildResult(
    ImmutableArray<ServiceRegistration> registrations,
    ImmutableArray<OpenGenericBypassWarning> warnings,
    ImmutableArray<Diagnostic> diagnostics
)
{
    public ImmutableArray<ServiceRegistration> Registrations { get; } = registrations;

    public ImmutableArray<OpenGenericBypassWarning> Warnings { get; } = warnings;

    public ImmutableArray<Diagnostic> Diagnostics { get; } = diagnostics;
}

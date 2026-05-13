using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
    private static string BuildGeneratedSource(
        ImmutableArray<ServiceRegistration> registrations,
        string projectNamespace,
        bool includeExcludeFromCodeCoverage
    )
    {
        var usings = includeExcludeFromCodeCoverage
            ? GenDISourceTemplates.UsingsWithCoverage
            : GenDISourceTemplates.UsingsWithoutCoverage;

        var excludeAttribute = includeExcludeFromCodeCoverage
            ? GenDISourceTemplates.ExcludeFromCodeCoverageAttribute + "\n"
            : string.Empty;

        var registrationLines = string.Join("\n", registrations.Select(BuildRegistrationLine));

        return GenDISourceTemplates
            .FileTemplate.Replace("{{USINGS}}", usings)
            .Replace("{{NAMESPACE}}", projectNamespace)
            .Replace("{{EXCLUDE_FROM_COVERAGE}}", excludeAttribute)
            .Replace("{{REGISTRATIONS}}", registrationLines);
    }

    private static string BuildRegistrationLine(ServiceRegistration registration)
    {
        var registrationMethod = registration.Lifetime switch
        {
            "ServiceLifetime.Singleton" => "Singleton",
            "ServiceLifetime.Scoped" => "Scoped",
            _ => "Transient",
        };

        if (string.IsNullOrWhiteSpace(registration.ThreadIsolationLifetime))
        {
            return BuildStandardRegistrationLine(registration, registrationMethod);
        }

        var threadIsolationMethod = registration.ThreadIsolationLifetime switch
        {
            "ServiceLifetime.Singleton" => "Singleton",
            "ServiceLifetime.Scoped" => "Scoped",
            _ => "Transient",
        };
        var cacheKey = string.Format(
            GenDISourceTemplates.ThreadIsolationCacheKeyTemplate,
            EscapeStringLiteral(registration.ServiceType),
            EscapeStringLiteral(registration.ImplementationType),
            EscapeStringLiteral(registration.KeyExpression ?? "nokey")
        );
        var cacheRegistration = string.Format(
            GenDISourceTemplates.ThreadIsolationCacheTemplate,
            threadIsolationMethod,
            registration.ServiceType,
            cacheKey,
            registration.FactoryBody
        );

        var accessRegistration = string.IsNullOrWhiteSpace(registration.KeyExpression)
            ? string.Format(
                GenDISourceTemplates.ThreadIsolationUnkeyedAccessTemplate,
                "Transient",
                registration.ServiceType,
                cacheKey
            )
            : string.Format(
                GenDISourceTemplates.ThreadIsolationKeyedAccessTemplate,
                "Transient",
                registration.ServiceType,
                registration.KeyExpression,
                cacheKey
            );

        var registrationStatement = $"{cacheRegistration}\n{accessRegistration}";
        if (string.IsNullOrWhiteSpace(registration.EnvironmentName))
        {
            return registrationStatement;
        }

        return string.Format(
            GenDISourceTemplates.ConditionalRegistrationTemplate,
            EscapeStringLiteral(registration.EnvironmentName),
            registrationStatement
        );
    }

    private static string BuildStandardRegistrationLine(
        ServiceRegistration registration,
        string registrationMethod
    )
    {
        var registrationStatement = string.Empty;
        if (string.IsNullOrWhiteSpace(registration.KeyExpression))
        {
            registrationStatement = string.Format(
                GenDISourceTemplates.UnkeyedRegistrationTemplate,
                registrationMethod,
                registration.ServiceType,
                registration.FactoryBody
            );
        }
        else
        {
            registrationStatement = string.Format(
                GenDISourceTemplates.KeyedRegistrationTemplate,
                registrationMethod,
                registration.ServiceType,
                registration.KeyExpression,
                registration.FactoryBody
            );
        }

        if (string.IsNullOrWhiteSpace(registration.EnvironmentName))
        {
            return registrationStatement;
        }

        return string.Format(
            GenDISourceTemplates.ConditionalRegistrationTemplate,
            EscapeStringLiteral(registration.EnvironmentName),
            registrationStatement
        );
    }

    private static string GetProjectNamespace(Compilation compilation)
    {
        var assemblyName = compilation.AssemblyName;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return "Generated";
        }

        var parts = assemblyName!
            .Split('.')
            .Select(static part => new string(
                part.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray()
            ))
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Length > 0 && char.IsDigit(part[0]) ? $"_{part}" : part)
            .ToImmutableArray();

        return parts.Length == 0 ? "Generated" : string.Join(".", parts);
    }
}

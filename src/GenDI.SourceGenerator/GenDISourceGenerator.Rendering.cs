using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
    private const string TransientRegistrationMethod = "Transient";

    private static string BuildGeneratedSource(
        ImmutableArray<ServiceRegistration> registrations,
        string projectNamespace,
        ImmutableArray<string> chainedExtensionCalls,
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
        var chainedCalls = string.Join(
            "\n",
            chainedExtensionCalls.Select(static chainedCall => $"        {chainedCall}")
        );

        return GenDISourceTemplates
            .FileTemplate.Replace("{{USINGS}}", usings)
            .Replace("{{NAMESPACE}}", projectNamespace)
            .Replace("{{EXCLUDE_FROM_COVERAGE}}", excludeAttribute)
            .Replace("{{CHAINED_CALLS}}", chainedCalls)
            .Replace("{{REGISTRATIONS}}", registrationLines);
    }

    private static string BuildRegistrationLine(ServiceRegistration registration)
    {
        var registrationMethod = registration.Lifetime switch
        {
            "ServiceLifetime.Singleton" => "Singleton",
            "ServiceLifetime.Scoped" => "Scoped",
            _ => TransientRegistrationMethod,
        };

        var registrationStatement = BuildRegistrationStatement(registration, registrationMethod);
        registrationStatement = WrapEnvironmentRegistration(registration, registrationStatement);

        return WrapModuleRegistration(registration, registrationStatement);
    }

    private static string WrapModuleRegistration(
        ServiceRegistration registration,
        string registrationStatement
    )
    {
        var moduleCondition = string.IsNullOrWhiteSpace(registration.ModuleName)
            ? "modules.Length == 0"
            : $"modules.Length == 0 || IsModuleEnabled(modules, \"{EscapeStringLiteral(registration.ModuleName)}\")";

        return string.Format(
            GenDISourceTemplates.ModuleRegistrationTemplate,
            moduleCondition,
            registrationStatement
        );
    }

    private static string WrapEnvironmentRegistration(
        ServiceRegistration registration,
        string registrationStatement
    )
    {
        return string.IsNullOrWhiteSpace(registration.EnvironmentName)
            ? registrationStatement
            : string.Format(
                GenDISourceTemplates.ConditionalRegistrationTemplate,
                EscapeStringLiteral(registration.EnvironmentName),
                registrationStatement
            );
    }

    private static string BuildRegistrationStatement(
        ServiceRegistration registration,
        string registrationMethod
    )
    {
        if (string.IsNullOrWhiteSpace(registration.ThreadIsolationLifetime))
        {
            return BuildStandardRegistrationStatement(registration, registrationMethod);
        }

        return BuildThreadIsolationRegistrationStatement(registration);
    }

    private static string BuildStandardRegistrationStatement(
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

        return registrationStatement;
    }

    private static string BuildThreadIsolationRegistrationStatement(ServiceRegistration registration)
    {
        var threadIsolationMethod = registration.ThreadIsolationLifetime switch
        {
            "ServiceLifetime.Singleton" => "Singleton",
            "ServiceLifetime.Scoped" => "Scoped",
            _ => TransientRegistrationMethod,
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
                TransientRegistrationMethod,
                registration.ServiceType,
                cacheKey
            )
            : string.Format(
                GenDISourceTemplates.ThreadIsolationKeyedAccessTemplate,
                TransientRegistrationMethod,
                registration.ServiceType,
                registration.KeyExpression,
                cacheKey
            );

        return $"{cacheRegistration}\n{accessRegistration}";
    }

    private static string GetProjectNamespace(Compilation compilation)
    {
        return GetProjectNamespace(compilation.AssemblyName);
    }

    private static string GetProjectNamespace(string? assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return "Generated";
        }

        var parts = assemblyName
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

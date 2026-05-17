using System.Collections.Immutable;
using GenDI.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDiSourceGenerator
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
            ? GenDiSourceTemplates.UsingsWithCoverage
            : GenDiSourceTemplates.UsingsWithoutCoverage;

        var excludeAttribute = includeExcludeFromCodeCoverage
            ? GenDiSourceTemplates.ExcludeFromCodeCoverageAttribute + "\n"
            : string.Empty;

        var registrationLines = string.Join("\n", registrations.Select(BuildRegistrationLine));
        var chainedCalls = string.Join(
            "\n",
            chainedExtensionCalls.Select(static chainedCall => $"        {chainedCall}")
        );

        return GenDiSourceTemplates
            .FileTemplate.Replace("{{USINGS}}", usings)
            .Replace("{{NAMESPACE}}", projectNamespace)
            .Replace("{{EXCLUDE_FROM_COVERAGE}}", excludeAttribute)
            .Replace("{{CHAINED_CALLS}}", chainedCalls)
            .Replace("{{REGISTRATIONS}}", registrationLines);
    }

    private static string BuildRegistrationLine(ServiceRegistration registration)
    {
        var registrationStatement = registration.DirectRegistrationStatement;
        if (string.IsNullOrWhiteSpace(registrationStatement))
        {
            var registrationMethod = GetRegistrationMethod(registration.Lifetime);
            registrationStatement = BuildRegistrationStatement(registration, registrationMethod);
        }
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
            GenDiSourceTemplates.ModuleRegistrationTemplate,
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
                GenDiSourceTemplates.ConditionalRegistrationTemplate,
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
        if (string.IsNullOrWhiteSpace(registration.KeyExpression))
        {
            if (!registration.UseTryAdd)
            {
                return string.Format(
                    GenDiSourceTemplates.UnkeyedAddRegistrationTemplate,
                    registrationMethod,
                    registration.ServiceType,
                    registration.FactoryBody
                );
            }

            if (!registration.AllowMultiple)
            {
                return string.Format(
                    GenDiSourceTemplates.UnkeyedTryAddRegistrationTemplate,
                    registrationMethod,
                    registration.ServiceType,
                    registration.FactoryBody
                );
            }

            return string.Format(
                GenDiSourceTemplates.UnkeyedTryAddMultipleGuardTemplate,
                registration.ServiceType,
                registration.ImplementationType,
                registrationMethod,
                registration.FactoryBody
            );
        }

        if (!registration.UseTryAdd)
        {
            return string.Format(
                GenDiSourceTemplates.KeyedAddRegistrationTemplate,
                registrationMethod,
                registration.ServiceType,
                registration.KeyExpression,
                registration.FactoryBody
            );
        }

        if (!registration.AllowMultiple)
        {
            return string.Format(
                GenDiSourceTemplates.KeyedTryAddRegistrationTemplate,
                registrationMethod,
                registration.ServiceType,
                registration.KeyExpression,
                registration.FactoryBody
            );
        }

        return string.Format(
            GenDiSourceTemplates.KeyedTryAddMultipleGuardTemplate,
            registration.ServiceType,
            registration.KeyExpression,
            registration.ImplementationType,
            registrationMethod,
            registration.FactoryBody
        );
    }

    private static string BuildThreadIsolationRegistrationStatement(ServiceRegistration registration)
    {
        var threadIsolationMethod = GetRegistrationMethod(registration.ThreadIsolationLifetime);
        var addPrefix = registration.UseTryAdd ? "TryAdd" : "Add";
        var cacheKey = string.Format(
            GenDiSourceTemplates.ThreadIsolationCacheKeyTemplate,
            EscapeStringLiteral(registration.ServiceType),
            EscapeStringLiteral(registration.ImplementationType),
            EscapeStringLiteral(registration.KeyExpression ?? "nokey")
        );
        var cacheRegistration = string.Format(
            GenDiSourceTemplates.ThreadIsolationCacheTemplate,
            $"{addPrefix}Keyed{threadIsolationMethod}",
            registration.ServiceType,
            cacheKey,
            registration.FactoryBody
        );

        var accessRegistration = string.IsNullOrWhiteSpace(registration.KeyExpression)
            ? string.Format(
                GenDiSourceTemplates.ThreadIsolationUnkeyedAccessTemplate,
                $"{addPrefix}{TransientRegistrationMethod}",
                registration.ServiceType,
                cacheKey
            )
            : string.Format(
                GenDiSourceTemplates.ThreadIsolationKeyedAccessTemplate,
                $"{addPrefix}Keyed{TransientRegistrationMethod}",
                registration.ServiceType,
                registration.KeyExpression,
                cacheKey
            );

        return $"{cacheRegistration}\n{accessRegistration}";
    }

    private static string GetRegistrationMethod(string? lifetime)
    {
        return lifetime switch
        {
            "ServiceLifetime.Singleton" => "Singleton",
            "ServiceLifetime.Scoped" => "Scoped",
            _ => TransientRegistrationMethod,
        };
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

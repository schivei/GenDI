using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using GenDI.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
    private const string TransientRegistrationMethod = "Transient";

    private static string BuildGeneratedSource(
        ImmutableArray<ServiceRegistration> registrations,
        string projectNamespace,
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

        return GenDiSourceTemplates
            .FileTemplate.Replace("{{USINGS}}", usings ?? string.Empty)
            .Replace("{{NAMESPACE}}", projectNamespace ?? string.Empty)
            .Replace("{{EXCLUDE_FROM_COVERAGE}}", excludeAttribute ?? string.Empty)
            .Replace("{{REGISTRATIONS}}", registrationLines ?? string.Empty)
            .Replace("{{DECORATOR_REGISTERS}}", registrations.Any(reg => reg.IsDecorator) ? GenDiSourceTemplates.DecoratorRegistersTemplate : string.Empty)
            .Replace("{{CHECK_MODULE}}", registrations.Any(reg => !string.IsNullOrWhiteSpace(reg.ModuleName)) ? GenDiSourceTemplates.CheckModuleTemplate : string.Empty)
            .Replace("{{TRY_ADD_MULTIPLE_GUARD}}", registrations.Any(reg => reg.AllowMultiple && reg.UseTryAdd && string.IsNullOrWhiteSpace(reg.KeyExpression)) ? GenDiSourceTemplates.TryAddMultipleGuardTemplate : string.Empty)
            .Replace("{{TRY_ADD_MULTIPLE_KEYED_GUARD}}", registrations.Any(reg => reg.AllowMultiple && reg.UseTryAdd && !string.IsNullOrWhiteSpace(reg.KeyExpression)) ? GenDiSourceTemplates.TryAddMultipleKeyedGuardTemplate : string.Empty) ?? string.Empty;
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
        var environments = registration.Environments.Where(env => !string.IsNullOrWhiteSpace(env.EnvironmentName))
            .OrderBy(env => env.NotEnvironment)
            .ThenBy(env => env.EnvironmentName)
            .ToImmutableArray();

        if (environments.Length == 0)
            return registrationStatement;

        var groups = environments.GroupBy(env => env.NotEnvironment);

        var notFalse = groups.Any(g => g.Key != true) ? groups.Where(g => g.Key != true).SelectMany(g =>
            g.Select(env =>
                string.Format(GenDiSourceTemplates.ConditionalRegistrationFilterTemplate, EscapeStringLiteral(env.EnvironmentName), string.Empty)
            )
        ).Aggregate(new StringBuilder(), (sb, condition) =>
            sb.Length == 0 ? sb.Append(condition) : sb.Append(" || ").Append(condition),
            sb => $"({sb})"
        ) : null;

        var notTrue = groups.Any(g => g.Key == true) ? groups.Where(g => g.Key == true).SelectMany(g =>
            g.Select(env =>
                string.Format(GenDiSourceTemplates.ConditionalRegistrationFilterTemplate, EscapeStringLiteral(env.EnvironmentName), "!")
            )
        ).Aggregate(new StringBuilder(), (sb, condition) =>
            sb.Length == 0 ? sb.Append(condition) : sb.Append(" && ").Append(condition),
            sb => $"({sb})"
        ) : null;

        var conditions = new[] { notFalse, notTrue }.Where(c => c is not null).ToArray();

        return string.Format(
            GenDiSourceTemplates.ConditionalRegistrationTemplate,
            string.Join(" && ", conditions),
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

    [ExcludeFromCodeCoverage]
    private static string BuildStandardRegistrationStatement(
        ServiceRegistration registration,
        string registrationMethod
    )
    {
        if (string.IsNullOrWhiteSpace(registration.KeyExpression))
        {
            if (registration.IsDecorator)
            {
                var body = ParseDecorationRecursion(registration.FactoryBody, registration.ServiceType, out var interceptedKey);

                if (interceptedKey is null)
                {
                    return string.Format(
                        GenDiSourceTemplates.UnkeyedAddDecoratorTemplate,
                        registration.ServiceType,
                        registrationMethod,
                        body
                    );
                }

                return string.Format(
                    GenDiSourceTemplates.KeyedAddDecoratorTemplate,
                    registration.ServiceType,
                    interceptedKey,
                    registrationMethod,
                    body
                );
            }

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

        if (registration.IsDecorator)
        {
            var body = ParseDecorationRecursion(registration.FactoryBody, registration.ServiceType, out var interceptedKey);

            return string.Format(
                GenDiSourceTemplates.KeyedAddDecoratorTemplate,
                registration.ServiceType,
                interceptedKey ?? registration.KeyExpression,
                registrationMethod,
                body
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

    [ExcludeFromCodeCoverage]
    private static object ParseDecorationRecursion(string factoryBody, string serviceType, out string? interceptedKeyExpression)
    {
        interceptedKeyExpression = null;

        string serviceTypeEscaped = Regex.Escape($"<{serviceType}>");

        string pattern = $@"^(.+?)(GetKeyedService|GetRequiredKeyedService|GetService|GetRequiredService)({serviceTypeEscaped}\()([^)]*)(\).*)$";

        var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(1));
        var match = regex.Match(factoryBody);

        if (!match.Success)
        {
            return factoryBody;
        }

        interceptedKeyExpression = $"{match.Groups[4].Value.Trim()}";
        if (string.IsNullOrWhiteSpace(interceptedKeyExpression))
        {
            interceptedKeyExpression = null;
        }

        var methodName = match.Groups[2].Value switch
        {
            "GetService" => "GetKeyedService",
            "GetRequiredService" => "GetRequiredKeyedService",
            _ => match.Groups[2].Value
        };

        return $"{match.Groups[1].Value}{methodName}{match.Groups[3].Value}internalKey{match.Groups[5].Value}";
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
                [.. part.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_')]
            ))
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Length > 0 && char.IsDigit(part[0]) ? $"_{part}" : part)
            .ToImmutableArray();

        return parts.Length == 0 ? "Generated" : string.Join(".", parts);
    }
}

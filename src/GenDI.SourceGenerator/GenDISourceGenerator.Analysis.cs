using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
    private static IEnumerable<ServiceRegistration> BuildRegistrations(INamedTypeSymbol symbol)
    {
        if (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract)
        {
            return Enumerable.Empty<ServiceRegistration>();
        }

        if (
            !TryGetInjectableAttribute(
                symbol,
                out var lifetime,
                out var explicitServiceType,
                out var order,
                out var group,
                out var keyExpression
            )
        )
        {
            return Enumerable.Empty<ServiceRegistration>();
        }

        var implementationType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var constructor = symbol
            .InstanceConstructors.Where(static constructorSymbol =>
                constructorSymbol.DeclaredAccessibility == Accessibility.Public
            )
            .OrderByDescending(static constructorSymbol => constructorSymbol.Parameters.Length)
            .FirstOrDefault();

        var serviceTypes = GetServiceTypes(symbol, implementationType, explicitServiceType);
        var factoryBody = BuildFactoryBody(symbol, implementationType, constructor);

        return serviceTypes.Select(serviceType => new ServiceRegistration(
            serviceType,
            implementationType,
            lifetime,
            factoryBody,
            order,
            group,
            keyExpression
        ));
    }

    private static bool TryGetInjectableAttribute(
        INamedTypeSymbol symbol,
        out string lifetime,
        out string? explicitServiceType,
        out int order,
        out int group,
        out string? keyExpression
    )
    {
        lifetime = "ServiceLifetime.Transient";
        explicitServiceType = null;
        order = DefaultOrderingValue;
        group = DefaultOrderingValue;
        keyExpression = null;

        foreach (var attributeData in symbol.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is null || !IsInjectableAttribute(attributeClass))
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length > 0)
            {
                var argument = attributeData.ConstructorArguments[0];
                if (argument.Value is int enumValue)
                {
                    lifetime = enumValue switch
                    {
                        0 => "ServiceLifetime.Singleton",
                        1 => "ServiceLifetime.Scoped",
                        _ => "ServiceLifetime.Transient",
                    };
                }
            }

            if (
                attributeClass.Arity == 1
                && attributeClass.TypeArguments[0] is ITypeSymbol serviceTypeSymbol
            )
            {
                explicitServiceType = serviceTypeSymbol.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                switch (namedArgument.Key)
                {
                    case "Order" when namedArgument.Value.Value is int orderValue:
                        order = orderValue;
                        break;
                    case "Group" when namedArgument.Value.Value is int groupValue:
                        group = groupValue;
                        break;
                    case "Key":
                        keyExpression = BuildTypedConstantExpression(namedArgument.Value);
                        break;
                }
            }

            return true;
        }

        return false;
    }

    private static ImmutableArray<string> GetServiceTypes(
        INamedTypeSymbol symbol,
        string implementationType,
        string? explicitServiceType
    )
    {
        var serviceTypes = new List<string>();
        var attributedServiceFound = false;

        if (!string.IsNullOrWhiteSpace(explicitServiceType))
        {
            var nonNullExplicitServiceType = explicitServiceType;
            if (nonNullExplicitServiceType is not null)
            {
                serviceTypes.Add(nonNullExplicitServiceType);
            }
        }

        foreach (var interfaceSymbol in symbol.AllInterfaces)
        {
            if (!HasServiceInjectionAttribute(interfaceSymbol))
            {
                continue;
            }

            attributedServiceFound = true;
            serviceTypes.Add(
                interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            );
        }

        var baseType = symbol.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (HasServiceInjectionAttribute(baseType))
            {
                attributedServiceFound = true;
                serviceTypes.Add(
                    baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                );
            }

            baseType = baseType.BaseType;
        }

        if (!attributedServiceFound)
        {
            serviceTypes.Add(implementationType);
        }

        return serviceTypes.Distinct(StringComparer.Ordinal).ToImmutableArray();
    }

    private static bool HasServiceInjectionAttribute(ITypeSymbol symbol)
    {
        return symbol
            .GetAttributes()
            .Any(attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == "GenDI.ServiceInjectionAttribute"
            );
    }

    private static string BuildFactoryBody(
        INamedTypeSymbol symbol,
        string implementationType,
        IMethodSymbol? constructor
    )
    {
        var parameters = BuildConstructorParameters(constructor);
        var injectableProperties = GetInjectableProperties(symbol);

        if (injectableProperties.Length == 0)
        {
            return $"new {implementationType}({parameters})";
        }

        var initializers = string.Join(
            "\n",
            injectableProperties.Select(property =>
                $"                @{property.Name} = {BuildResolutionExpression(property.Type, property.KeyExpression)},"
            )
        );

        return $"new {implementationType}({parameters})\n            {{\n{initializers}\n            }}";
    }

    private static string BuildConstructorParameters(IMethodSymbol? constructor)
    {
        if (constructor is null || constructor.Parameters.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            constructor.Parameters.Select(parameter =>
            {
                var parameterType = parameter.Type.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
                var keyExpression = GetFromKeyedServicesKey(parameter);
                return BuildResolutionExpression(parameterType, keyExpression);
            })
        );
    }

    private static ImmutableArray<InjectablePropertyInfo> GetInjectableProperties(
        INamedTypeSymbol symbol
    )
    {
        return symbol
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(IsInjectableInitProperty)
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .Select(property =>
            {
                var propertyType = property.Type.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
                var keyExpression =
                    GetInjectAttributeKey(property) ?? GetFromKeyedServicesKey(property);
                return new InjectablePropertyInfo(property.Name, propertyType, keyExpression);
            })
            .ToImmutableArray();
    }

    private static bool IsInjectableInitProperty(IPropertySymbol property)
    {
        if (
            property.IsStatic
            || property.GetMethod is null
            || property.SetMethod is null
            || !property.SetMethod.IsInitOnly
        )
        {
            return false;
        }

        if (property.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            return false;
        }

        if (
            property.SetMethod.DeclaredAccessibility
            is not (Accessibility.Public or Accessibility.Internal)
        )
        {
            return false;
        }

        return property
            .GetAttributes()
            .Any(attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == "GenDI.InjectAttribute"
            );
    }

    private static string BuildResolutionExpression(
        string fullyQualifiedType,
        string? keyExpression
    )
    {
        return string.IsNullOrWhiteSpace(keyExpression)
            ? $"serviceProvider.GetRequiredService<{fullyQualifiedType}>()"
            : $"serviceProvider.GetRequiredKeyedService<{fullyQualifiedType}>({keyExpression})";
    }

    private static string? GetInjectAttributeKey(IPropertySymbol property)
    {
        foreach (var attributeData in property.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != "GenDI.InjectAttribute")
            {
                continue;
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "Key")
                {
                    return BuildTypedConstantExpression(namedArgument.Value);
                }
            }
        }

        return null;
    }

    private static string? GetFromKeyedServicesKey(ISymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (
                attributeData.AttributeClass?.ToDisplayString()
                != "Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute"
            )
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length > 0)
            {
                return BuildTypedConstantExpression(attributeData.ConstructorArguments[0]);
            }
        }

        return null;
    }

    private static bool IsGeneratedCodeCoverageEnabled(Compilation compilation)
    {
        foreach (var attributeData in compilation.Assembly.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != "GenDI.GenDICoverationAttribute")
            {
                continue;
            }

            if (
                attributeData.ConstructorArguments.Length > 0
                && attributeData.ConstructorArguments[0].Value
                    is bool includeGeneratedCodeInCoverage
            )
            {
                return includeGeneratedCodeInCoverage;
            }

            return true;
        }

        return true;
    }

    private static string? BuildTypedConstantExpression(TypedConstant typedConstant)
    {
        if (typedConstant.IsNull)
        {
            return "null";
        }

        var value = typedConstant.Value;
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            string s => $"\"{EscapeStringLiteral(s)}\"",
            char c => $"'{EscapeCharLiteral(c)}'",
            bool b => b ? "true" : "false",
            byte or sbyte or short or ushort or int or uint or long or ulong => Convert.ToString(
                value,
                CultureInfo.InvariantCulture
            ),
            float f => f.ToString(CultureInfo.InvariantCulture) + "F",
            double d => d.ToString(CultureInfo.InvariantCulture) + "D",
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "M",
            _ => BuildEnumConstantExpression(typedConstant),
        };
    }

    private static string? BuildEnumConstantExpression(TypedConstant typedConstant)
    {
        if (typedConstant.Type?.TypeKind != TypeKind.Enum || typedConstant.Value is null)
        {
            return null;
        }

        var enumType = typedConstant.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return $"({enumType}){Convert.ToInt64(typedConstant.Value, CultureInfo.InvariantCulture)}";
    }

    private static string EscapeStringLiteral(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static string EscapeCharLiteral(char value)
    {
        return value switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => value.ToString(),
        };
    }

    private sealed class InjectablePropertyInfo
    {
        public InjectablePropertyInfo(string name, string type, string? keyExpression)
        {
            Name = name;
            Type = type;
            KeyExpression = keyExpression;
        }

        public string Name { get; }

        public string Type { get; }

        public string? KeyExpression { get; }
    }
}

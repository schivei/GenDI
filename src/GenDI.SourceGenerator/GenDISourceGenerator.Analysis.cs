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
        var environmentName = GetConditionalEnvironmentName(symbol);

        return serviceTypes.Select(serviceType => new ServiceRegistration(
            serviceType.ServiceType,
            implementationType,
            ResolveRegistrationLifetime(lifetime, serviceType.FallbackLifetime),
            factoryBody,
            order,
            group,
            keyExpression,
            environmentName
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
                lifetime = ConvertLifetimeEnumToExpression(attributeData.ConstructorArguments[0]);
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

    private static ImmutableArray<ServiceContractTarget> GetServiceTypes(
        INamedTypeSymbol symbol,
        string implementationType,
        string? explicitServiceType
    )
    {
        var serviceTypes = new List<ServiceContractTarget>();
        var hasAnyContract = false;

        if (!string.IsNullOrWhiteSpace(explicitServiceType))
        {
            var nonNullExplicitServiceType = explicitServiceType;
            if (nonNullExplicitServiceType is not null)
            {
                serviceTypes.Add(new ServiceContractTarget(nonNullExplicitServiceType, null));
                hasAnyContract = true;
            }
        }

        foreach (var interfaceSymbol in symbol.AllInterfaces)
        {
            if (!HasServiceInjectionAttribute(interfaceSymbol))
            {
                continue;
            }

            hasAnyContract = true;
            serviceTypes.Add(
                new ServiceContractTarget(
                    interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    TryGetServiceInjectionLifetime(interfaceSymbol)
                )
            );
        }

        var baseType = symbol.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (HasServiceInjectionAttribute(baseType))
            {
                hasAnyContract = true;
                serviceTypes.Add(
                    new ServiceContractTarget(
                        baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        TryGetServiceInjectionLifetime(baseType)
                    )
                );
            }

            baseType = baseType.BaseType;
        }

        if (!hasAnyContract)
        {
            serviceTypes.Add(new ServiceContractTarget(implementationType, null));
        }

        return serviceTypes
            .GroupBy(static target => target.ServiceType, StringComparer.Ordinal)
            .Select(static group =>
                group.FirstOrDefault(static target =>
                    !string.IsNullOrWhiteSpace(target.FallbackLifetime)
                ) ?? group.First()
            )
            .ToImmutableArray();
    }

    private static bool HasServiceInjectionAttribute(ITypeSymbol symbol)
    {
        return symbol
            .GetAttributes()
            .Any(attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == "GenDI.ServiceInjectionAttribute"
            );
    }

    private static string ResolveRegistrationLifetime(
        string injectableLifetime,
        string? fallbackLifetime
    )
    {
        if (injectableLifetime != "ServiceLifetime.Transient")
        {
            return injectableLifetime;
        }

        return string.IsNullOrWhiteSpace(fallbackLifetime) ? injectableLifetime : fallbackLifetime;
    }

    private static string? TryGetServiceInjectionLifetime(ITypeSymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (
                attributeData.AttributeClass?.ToDisplayString() != "GenDI.ServiceInjectionAttribute"
            )
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length > 0)
            {
                return ConvertLifetimeEnumToExpression(attributeData.ConstructorArguments[0]);
            }

            return "ServiceLifetime.Transient";
        }

        return null;
    }

    private static string? GetConditionalEnvironmentName(INamedTypeSymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (
                attributeData.AttributeClass?.ToDisplayString()
                != "GenDI.ConditionalInjectableAttribute"
            )
            {
                continue;
            }

            if (
                attributeData.ConstructorArguments.Length > 0
                && attributeData.ConstructorArguments[0].Value is string environmentName
                && !string.IsNullOrWhiteSpace(environmentName)
            )
            {
                return environmentName;
            }
        }

        return null;
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
                $"                @{property.Name} = {BuildResolutionExpression(property.Type, property.KeyExpression, property.UseOptionalResolution)},"
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
                return BuildResolutionExpression(
                    parameterType,
                    keyExpression,
                    ShouldUseOptionalResolution(parameter.Type)
                );
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
                var injectMetadata = GetInjectPropertyMetadata(property);
                var keyExpression =
                    injectMetadata.KeyExpression ?? GetFromKeyedServicesKey(property);
                return new InjectablePropertyInfo(
                    property.Name,
                    propertyType,
                    keyExpression,
                    injectMetadata.HasInjectOptionalAttribute
                        || ShouldUseOptionalResolution(property.Type)
                );
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
            .Any(attributeData => IsInjectPropertyAttribute(attributeData.AttributeClass));
    }

    private static bool IsInjectPropertyAttribute(INamedTypeSymbol? attributeClass)
    {
        var attributeDisplayName = attributeClass?.ToDisplayString();
        return attributeDisplayName is "GenDI.InjectAttribute" or "GenDI.InjectOptionalAttribute";
    }

    private static string ConvertLifetimeEnumToExpression(TypedConstant argument)
    {
        if (argument.Value is not int enumValue)
        {
            return "ServiceLifetime.Transient";
        }

        return enumValue switch
        {
            0 => "ServiceLifetime.Singleton",
            1 => "ServiceLifetime.Scoped",
            _ => "ServiceLifetime.Transient",
        };
    }

    private static string BuildResolutionExpression(
        string fullyQualifiedType,
        string? keyExpression,
        bool useOptionalResolution
    )
    {
        if (string.IsNullOrWhiteSpace(keyExpression))
        {
            return useOptionalResolution
                ? $"serviceProvider.GetService<{fullyQualifiedType}>()"
                : $"serviceProvider.GetRequiredService<{fullyQualifiedType}>()";
        }

        return useOptionalResolution
            ? $"serviceProvider.GetKeyedService<{fullyQualifiedType}>({keyExpression})"
            : $"serviceProvider.GetRequiredKeyedService<{fullyQualifiedType}>({keyExpression})";
    }

    private static bool ShouldUseOptionalResolution(ITypeSymbol typeSymbol)
    {
        // NullableAnnotation.None means the symbol comes from an oblivious context
        // (for example, nullable disabled in the consumer assembly).
        // In this mode we prefer optional resolution to avoid assuming non-null.
        return typeSymbol.NullableAnnotation
            is NullableAnnotation.Annotated
                or NullableAnnotation.None;
    }

    private static InjectPropertyMetadata GetInjectPropertyMetadata(IPropertySymbol property)
    {
        var keyExpression = default(string);
        var hasInjectOptionalAttribute = false;

        foreach (var attributeData in property.GetAttributes())
        {
            var attributeDisplayName = attributeData.AttributeClass?.ToDisplayString();
            if (
                attributeDisplayName
                is not ("GenDI.InjectAttribute" or "GenDI.InjectOptionalAttribute")
            )
            {
                continue;
            }

            if (attributeDisplayName == "GenDI.InjectOptionalAttribute")
            {
                hasInjectOptionalAttribute = true;
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "Key")
                {
                    keyExpression = BuildTypedConstantExpression(namedArgument.Value);
                }
            }
        }

        return new InjectPropertyMetadata(keyExpression, hasInjectOptionalAttribute);
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
            float f => BuildFloatConstantExpression(f),
            double d => BuildDoubleConstantExpression(d),
            _ => null,
        };
    }

    private static string BuildFloatConstantExpression(float value)
    {
        return value switch
        {
            _ when float.IsNaN(value) => "float.NaN",
            _ when float.IsPositiveInfinity(value) => "float.PositiveInfinity",
            _ when float.IsNegativeInfinity(value) => "float.NegativeInfinity",
            _ => value.ToString(CultureInfo.InvariantCulture) + "F",
        };
    }

    private static string BuildDoubleConstantExpression(double value)
    {
        return value switch
        {
            _ when double.IsNaN(value) => "double.NaN",
            _ when double.IsPositiveInfinity(value) => "double.PositiveInfinity",
            _ when double.IsNegativeInfinity(value) => "double.NegativeInfinity",
            _ => value.ToString(CultureInfo.InvariantCulture) + "D",
        };
    }

    private static string EscapeStringLiteral(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(
                character switch
                {
                    '\"' => "\\\"",
                    '\\' => "\\\\",
                    '\0' => "\\0",
                    '\a' => "\\a",
                    '\b' => "\\b",
                    '\f' => "\\f",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    '\v' => "\\v",
                    _ when char.IsControl(character) => $"\\u{(int)character:X4}",
                    _ => character.ToString(),
                }
            );
        }

        return builder.ToString();
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
        public InjectablePropertyInfo(
            string name,
            string type,
            string? keyExpression,
            bool useOptionalResolution
        )
        {
            Name = name;
            Type = type;
            KeyExpression = keyExpression;
            UseOptionalResolution = useOptionalResolution;
        }

        public string Name { get; }

        public string Type { get; }

        public string? KeyExpression { get; }

        public bool UseOptionalResolution { get; }
    }

    private sealed class InjectPropertyMetadata
    {
        public InjectPropertyMetadata(string? keyExpression, bool hasInjectOptionalAttribute)
        {
            KeyExpression = keyExpression;
            HasInjectOptionalAttribute = hasInjectOptionalAttribute;
        }

        public string? KeyExpression { get; }

        public bool HasInjectOptionalAttribute { get; }
    }
}

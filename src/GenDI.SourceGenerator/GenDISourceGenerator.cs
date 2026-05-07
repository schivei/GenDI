using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenDI.SourceGenerator;

[Generator]
public sealed class GenDISourceGenerator : IIncrementalGenerator
{
    private const int DefaultOrderingValue = int.MaxValue;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) =>
                    node is ClassDeclarationSyntax classDeclaration &&
                    HasInjectableAttributeSyntax(classDeclaration),
                static (generatorContext, _) => generatorContext.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)generatorContext.Node) as INamedTypeSymbol)
            .Where(static symbol => symbol is not null)
            .Select(static (symbol, _) => symbol!)
            .Collect();

        var generationInput = context.CompilationProvider.Combine(candidates);

        context.RegisterSourceOutput(generationInput, static (sourceProductionContext, source) =>
        {
            var (compilation, symbols) = source;
            var registrations = symbols
                .SelectMany(BuildRegistrations)
                .Distinct(ServiceRegistrationComparer.Instance)
                .OrderBy(static registration => registration.Group)
                .ThenBy(static registration => registration.Order)
                .ThenBy(static registration => registration.ServiceType, StringComparer.Ordinal)
                .ToImmutableArray();

            if (registrations.Length == 0)
            {
                return;
            }

            sourceProductionContext.AddSource(
                "GenDIServiceCollectionExtensions.g.cs",
                BuildGeneratedSource(registrations, GetProjectNamespace(compilation), includeExcludeFromCodeCoverage: !IsGeneratedCodeCoverageEnabled(compilation)));
        });
    }

    private static IEnumerable<ServiceRegistration> BuildRegistrations(INamedTypeSymbol symbol)
    {
        if (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract)
        {
            return Enumerable.Empty<ServiceRegistration>();
        }

        if (!TryGetInjectableAttribute(symbol, out var lifetime, out var explicitServiceType, out var order, out var group))
        {
            return Enumerable.Empty<ServiceRegistration>();
        }

        var implementationType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var constructor = symbol.InstanceConstructors
            .Where(static constructorSymbol => constructorSymbol.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(static constructorSymbol => constructorSymbol.Parameters.Length)
            .FirstOrDefault();

        var serviceTypes = GetServiceTypes(symbol, implementationType, explicitServiceType);
        var factoryBody = BuildFactoryBody(symbol, implementationType, constructor);

        return serviceTypes.Select(serviceType => new ServiceRegistration(serviceType, implementationType, lifetime, factoryBody, order, group));
    }

    private static bool TryGetInjectableAttribute(
        INamedTypeSymbol symbol,
        out string lifetime,
        out string? explicitServiceType,
        out int order,
        out int group)
    {
        lifetime = "ServiceLifetime.Transient";
        explicitServiceType = null;
        order = DefaultOrderingValue;
        group = DefaultOrderingValue;

        foreach (var attributeData in symbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != "GenDI.InjectableAttribute")
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
                        _ => "ServiceLifetime.Transient"
                    };
                }
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                switch (namedArgument.Key)
                {
                    case "ServiceType" when namedArgument.Value.Value is INamedTypeSymbol serviceTypeSymbol:
                        explicitServiceType = serviceTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        break;
                    case "Order" when namedArgument.Value.Value is int orderValue:
                        order = orderValue;
                        break;
                    case "Group" when namedArgument.Value.Value is int groupValue:
                        group = groupValue;
                        break;
                }
            }

            return true;
        }

        return false;
    }

    private static ImmutableArray<string> GetServiceTypes(INamedTypeSymbol symbol, string implementationType, string? explicitServiceType)
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
            serviceTypes.Add(interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        var baseType = symbol.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (HasServiceInjectionAttribute(baseType))
            {
                attributedServiceFound = true;
                serviceTypes.Add(baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            baseType = baseType.BaseType;
        }

        if (!attributedServiceFound)
        {
            // When no abstraction is explicitly marked with [ServiceInjection],
            // register the concrete type as its own service contract.
            serviceTypes.Add(implementationType);
        }

        return serviceTypes
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static bool HasServiceInjectionAttribute(ITypeSymbol symbol)
    {
        return symbol.GetAttributes()
            .Any(attributeData => attributeData.AttributeClass?.ToDisplayString() == "GenDI.ServiceInjectionAttribute");
    }

    private static bool HasInjectableAttributeSyntax(ClassDeclarationSyntax classDeclaration)
    {
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName is "Injectable" or "InjectableAttribute" or "GenDI.Injectable" or "GenDI.InjectableAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string BuildFactoryBody(INamedTypeSymbol symbol, string implementationType, IMethodSymbol? constructor)
    {
        var parameters = constructor is null || constructor.Parameters.Length == 0
            ? string.Empty
            : string.Join(
                ", ",
                constructor.Parameters.Select(parameter =>
                {
                    var parameterType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    return $"serviceProvider.GetRequiredService<{parameterType}>()";
                }));

        var injectableProperties = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(IsInjectableInitProperty)
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .ToImmutableArray();

        if (injectableProperties.Length == 0)
        {
            return $"new {implementationType}({parameters})";
        }

        var initializers = string.Join(
            "\n",
            injectableProperties.Select(property =>
            {
                var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"                @{property.Name} = serviceProvider.GetRequiredService<{propertyType}>(),";
            }));

        return $"new {implementationType}({parameters})\n            {{\n{initializers}\n            }}";
    }

    private static bool IsInjectableInitProperty(IPropertySymbol property)
    {
        if (property.IsStatic || property.GetMethod is null || property.SetMethod is null || !property.SetMethod.IsInitOnly)
        {
            return false;
        }

        if (property.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            return false;
        }

        if (property.SetMethod.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            return false;
        }

        return property.GetAttributes()
            .Any(attributeData => attributeData.AttributeClass?.ToDisplayString() == "GenDI.InjectAttribute");
    }

    private static bool IsGeneratedCodeCoverageEnabled(Compilation compilation)
    {
        foreach (var attributeData in compilation.Assembly.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != "GenDI.GenDICoverationAttribute")
            {
                continue;
            }

            if (attributeData.ConstructorArguments.Length > 0 && attributeData.ConstructorArguments[0].Value is bool includeGeneratedCodeInCoverage)
            {
                return includeGeneratedCodeInCoverage;
            }

            return true;
        }

        return true;
    }

    private static string BuildGeneratedSource(ImmutableArray<ServiceRegistration> registrations, string projectNamespace, bool includeExcludeFromCodeCoverage)
    {
        var source = new StringBuilder();
        source.Append(
            "// <auto-generated />\n" +
            "#nullable enable\n" +
            "using System;\n");

        if (includeExcludeFromCodeCoverage)
        {
            source.Append("using System.Diagnostics.CodeAnalysis;\n");
        }

        source.Append(
            "using Microsoft.Extensions.DependencyInjection;\n\n" +
            "namespace " + projectNamespace + ".DependencyInjection;\n\n" +
            (includeExcludeFromCodeCoverage ? "[ExcludeFromCodeCoverage]\n" : string.Empty) +
            "public static class GenDIServiceCollectionExtensions\n" +
            "{\n" +
            "    public static IServiceCollection AddGenDIServices(this IServiceCollection services)\n" +
            "    {\n" +
            "        if (services is null)\n" +
            "        {\n" +
            "            throw new ArgumentNullException(nameof(services));\n" +
            "        }\n");

        foreach (var registration in registrations)
        {
            source.AppendLine(
                $"        services.Add(new ServiceDescriptor(typeof({registration.ServiceType}), static serviceProvider => {registration.FactoryBody}, {registration.Lifetime}));");
        }

        source.Append(
            """
            
                    return services;
                }
            }
            """);

        return source.ToString();
    }

    private static string GetProjectNamespace(Compilation compilation)
    {
        var assemblyName = compilation.AssemblyName;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return "Generated";
        }

        var sanitizedAssemblyName = assemblyName!;
        var parts = sanitizedAssemblyName
            .Split('.')
            .Select(static part => new string(part.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray()))
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .Select(static part => part.Length > 0 && char.IsDigit(part[0]) ? $"_{part}" : part)
            .ToImmutableArray();

        return parts.Length == 0 ? "Generated" : string.Join(".", parts);
    }

    private sealed class ServiceRegistration
    {
        public ServiceRegistration(string serviceType, string implementationType, string lifetime, string factoryBody, int order, int group)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            FactoryBody = factoryBody;
            Order = order;
            Group = group;
        }

        public string ServiceType { get; }

        public string ImplementationType { get; }

        public string Lifetime { get; }

        public string FactoryBody { get; }

        public int Order { get; }

        public int Group { get; }
    }

    private sealed class ServiceRegistrationComparer : IEqualityComparer<ServiceRegistration>
    {
        public static ServiceRegistrationComparer Instance { get; } = new();

        public bool Equals(ServiceRegistration? x, ServiceRegistration? y)
        {
            return x?.ServiceType == y?.ServiceType && x?.ImplementationType == y?.ImplementationType;
        }

        public int GetHashCode(ServiceRegistration obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            unchecked
            {
                return ((obj.ServiceType?.GetHashCode() ?? 0) * 397) ^ (obj.ImplementationType?.GetHashCode() ?? 0);
            }
        }
    }
}

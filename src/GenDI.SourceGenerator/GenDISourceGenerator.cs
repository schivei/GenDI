using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenDI.SourceGenerator;

[Generator]
public sealed class GenDISourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) =>
                    node is ClassDeclarationSyntax classDeclaration &&
                    (classDeclaration.BaseList is not null || classDeclaration.AttributeLists.Count > 0),
                static (generatorContext, _) => generatorContext.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)generatorContext.Node) as INamedTypeSymbol)
            .Where(static symbol => symbol is not null)
            .Select(static (symbol, _) => symbol!)
            .Collect();

        context.RegisterSourceOutput(candidates, static (sourceProductionContext, symbols) =>
        {
            var registrations = symbols
                .Select(BuildRegistration)
                .OfType<ServiceRegistration>()
                .Distinct(ServiceRegistrationComparer.Instance)
                .ToImmutableArray();

            if (registrations.Length == 0)
            {
                return;
            }

            sourceProductionContext.AddSource(
                "GenDIRegistration.g.cs",
                BuildGeneratedSource(registrations));
        });
    }

    private static ServiceRegistration? BuildRegistration(INamedTypeSymbol symbol)
    {
        if (symbol.TypeKind != TypeKind.Class || symbol.IsAbstract)
        {
            return null;
        }

        var hasInjectableAttribute = TryGetInjectableAttribute(symbol, out var lifetime, out var explicitServiceType);
        var markerLifetime = GetMarkerLifetime(symbol);
        if (!hasInjectableAttribute && markerLifetime is null)
        {
            return null;
        }

        lifetime ??= markerLifetime ?? "ServiceLifetime.Transient";
        var implementationType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var serviceType = explicitServiceType
            ?? GetDefaultServiceType(symbol)
            ?? implementationType;
        var constructor = symbol.InstanceConstructors
            .Where(static constructorSymbol => constructorSymbol.DeclaredAccessibility == Accessibility.Public)
            .OrderByDescending(static constructorSymbol => constructorSymbol.Parameters.Length)
            .FirstOrDefault();

        var factoryBody = BuildFactoryBody(implementationType, constructor);
        return new ServiceRegistration(serviceType, implementationType, lifetime, factoryBody);
    }

    private static bool TryGetInjectableAttribute(
        INamedTypeSymbol symbol,
        out string? lifetime,
        out string? explicitServiceType)
    {
        lifetime = null;
        explicitServiceType = null;

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
                if (namedArgument.Key != "ServiceType" || namedArgument.Value.Value is not INamedTypeSymbol serviceTypeSymbol)
                {
                    continue;
                }

                explicitServiceType = serviceTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            return true;
        }

        return false;
    }

    private static string? GetMarkerLifetime(INamedTypeSymbol symbol)
    {
        foreach (var interfaceSymbol in symbol.AllInterfaces)
        {
            switch (interfaceSymbol.ToDisplayString())
            {
                case "GenDI.ISingletonInjectable":
                    return "ServiceLifetime.Singleton";
                case "GenDI.IScopedInjectable":
                    return "ServiceLifetime.Scoped";
                case "GenDI.ITransientInjectable":
                case "GenDI.IInjectable":
                    return "ServiceLifetime.Transient";
            }
        }

        return null;
    }

    private static string? GetDefaultServiceType(INamedTypeSymbol symbol)
    {
        foreach (var interfaceSymbol in symbol.AllInterfaces)
        {
            var name = interfaceSymbol.ToDisplayString();
            if (name.StartsWith("GenDI.", StringComparison.Ordinal))
            {
                continue;
            }

            return interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return null;
    }

    private static string BuildFactoryBody(string implementationType, IMethodSymbol? constructor)
    {
        if (constructor is null || constructor.Parameters.Length == 0)
        {
            return $"new {implementationType}()";
        }

        var parameters = string.Join(
            ", ",
            constructor.Parameters.Select(parameter =>
            {
                var parameterType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"({parameterType})GenDIResolutionPipeline.ResolveRequiredOrFallback(serviceProvider, typeof({parameterType}))";
            }));

        return $"new {implementationType}({parameters})";
    }

    private static string BuildGeneratedSource(ImmutableArray<ServiceRegistration> registrations)
    {
        var source = new StringBuilder(
            """
            // <auto-generated />
            #nullable enable
            using GenDI.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection;

            namespace GenDI.DependencyInjection;

            internal static partial class GenDIRegistration
            {
                static partial void RegisterGenerated(IServiceCollection services)
                {
            """);

        foreach (var registration in registrations)
        {
            source.AppendLine(
                $"        GenDIResolutionPipeline.RegisterGeneratedFactory(typeof({registration.ImplementationType}), {registration.Lifetime}, static serviceProvider => {registration.FactoryBody});");
            source.AppendLine(
                $"        services.Add(new ServiceDescriptor(typeof({registration.ServiceType}), static serviceProvider => ({registration.ServiceType})GenDIResolutionPipeline.ResolveOrCreate(serviceProvider, typeof({registration.ImplementationType})), {registration.Lifetime}));");
        }

        source.Append(
            """
                }
            }
            """);

        return source.ToString();
    }

    private sealed class ServiceRegistration
    {
        public ServiceRegistration(string serviceType, string implementationType, string lifetime, string factoryBody)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
            FactoryBody = factoryBody;
        }

        public string ServiceType { get; }

        public string ImplementationType { get; }

        public string Lifetime { get; }

        public string FactoryBody { get; }
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

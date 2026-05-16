using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;

namespace GenDI.SourceGenerator;

public sealed partial class GenDISourceGenerator
{
    private const string TransientLifetimeExpression = "ServiceLifetime.Transient";
    private const string SingletonLifetimeExpression = "ServiceLifetime.Singleton";

    private static RegistrationBuildResult BuildRegistrations(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> allTypes
    )
    {
        var concreteTypes = allTypes
            .Where(typeSymbol =>
                typeSymbol.TypeKind == TypeKind.Class
                && !typeSymbol.IsAbstract
                && IsTypeDeclarationAccessibleFromGeneratedCode(typeSymbol, compilation)
            )
            .ToImmutableArray();
        var registrations = new List<ServiceRegistration>();
        var warnings = new List<OpenGenericBypassWarning>();
        var injectableTypes = new Dictionary<INamedTypeSymbol, InjectableMetadata>(
            SymbolEqualityComparer.Default
        );

        warnings.AddRange(CollectDecoratorOpenGenericWarnings(concreteTypes));

        foreach (var concreteType in concreteTypes)
        {
            if (HasDecoratorTarget(compilation, concreteType))
            {
                continue;
            }

            if (!TryGetInjectableAttribute(concreteType, out var injectableMetadata))
            {
                continue;
            }

            if (!IsClosedType(concreteType))
            {
                warnings.Add(
                    BuildOpenGenericBypassWarning(concreteType, "Injectable class registration")
                );
                continue;
            }

            if (injectableMetadata.HasOpenGenericExplicitServiceType)
            {
                warnings.Add(
                    BuildOpenGenericBypassWarning(
                        concreteType,
                        "Injectable explicit service contract"
                    )
                );
                continue;
            }

            injectableTypes[concreteType] = injectableMetadata;
            registrations.AddRange(
                BuildDirectRegistrations(compilation, concreteType, injectableMetadata, warnings)
            );
        }

        registrations.AddRange(
            BuildIndirectRegistrations(
                compilation,
                concreteTypes,
                injectableTypes,
                registrations,
                warnings
            )
        );
        registrations.AddRange(BuildFactoryRegistrations(compilation, concreteTypes, warnings));
        ApplyDecorators(compilation, concreteTypes, registrations, warnings);
        var chainedExtensionCalls = BuildChainedExtensionCalls(compilation);

        return new RegistrationBuildResult(
            registrations.ToImmutableArray(),
            chainedExtensionCalls,
            warnings
                .Where(static warning => warning.Location is { IsInSource: true })
                .GroupBy(static warning =>
                    (
                        warning.Location.GetLineSpan().Path,
                        warning.Location.SourceSpan.Start,
                        warning.Context,
                        warning.TypeDisplay
                    )
                )
                .Select(static group => group.First())
                .ToImmutableArray()
        );
    }

    private static ImmutableArray<string> BuildChainedExtensionCalls(Compilation compilation)
    {
        var explicitlyChainedNamespaces = GetExplicitlyChainedDependencyNamespaces(compilation);
        var chainedCalls = ImmutableArray.CreateBuilder<string>();

        foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            if (!ShouldScanReferencedAssembly(referencedAssembly.Name))
            {
                continue;
            }

            var dependencyNamespace = GetProjectNamespace(referencedAssembly.Name);
            if (
                string.IsNullOrWhiteSpace(dependencyNamespace)
                || explicitlyChainedNamespaces.Contains(dependencyNamespace)
            )
            {
                continue;
            }

            if (!HasGeneratedAddGenDIServicesMethod(referencedAssembly, dependencyNamespace))
            {
                continue;
            }

            chainedCalls.Add(
                $"global::{dependencyNamespace}.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(services, modules);"
            );
        }

        return chainedCalls
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static call => call, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static HashSet<string> GetExplicitlyChainedDependencyNamespaces(Compilation compilation)
    {
        var explicitlyChainedNamespaces = new HashSet<string>(StringComparer.Ordinal);

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var sourceText = syntaxTree.GetText().ToString();
            foreach (var referencedAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                if (!ShouldScanReferencedAssembly(referencedAssembly.Name))
                {
                    continue;
                }

                var dependencyNamespace = GetProjectNamespace(referencedAssembly.Name);
                if (string.IsNullOrWhiteSpace(dependencyNamespace))
                {
                    continue;
                }

                if (
                    sourceText.Contains(
                        $"{dependencyNamespace}.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(",
                        StringComparison.Ordinal
                    )
                    || sourceText.Contains(
                        $"global::{dependencyNamespace}.DependencyInjection.GenDIServiceCollectionExtensions.AddGenDIServices(",
                        StringComparison.Ordinal
                    )
                )
                {
                    explicitlyChainedNamespaces.Add(dependencyNamespace);
                }
            }
        }

        return explicitlyChainedNamespaces;
    }

    private static bool HasGeneratedAddGenDIServicesMethod(
        IAssemblySymbol assemblySymbol,
        string dependencyNamespace
    )
    {
        var dependencyInjectionNamespace = GetNamespaceSymbol(
            assemblySymbol.GlobalNamespace,
            $"{dependencyNamespace}.DependencyInjection"
        );
        if (dependencyInjectionNamespace is null)
        {
            return false;
        }

        var extensionType = dependencyInjectionNamespace.GetTypeMembers(
            "GenDIServiceCollectionExtensions"
        );
        foreach (var typeMember in extensionType)
        {
            foreach (var method in typeMember.GetMembers("AddGenDIServices").OfType<IMethodSymbol>())
            {
                if (
                    method is { IsStatic: true, MethodKind: MethodKind.Ordinary }
                    && method.Parameters.Length == 2
                    && method.Parameters[0].Type.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.IServiceCollection"
                    && method.Parameters[1].Type is IArrayTypeSymbol arrayType
                    && arrayType.ElementType.SpecialType == SpecialType.System_String
                )
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static INamespaceSymbol? GetNamespaceSymbol(
        INamespaceSymbol rootNamespace,
        string fullNamespace
    )
    {
        var currentNamespace = rootNamespace;
        foreach (
            var namespaceSegment in fullNamespace.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            var nextNamespace = currentNamespace.GetNamespaceMembers().FirstOrDefault(
                namespaceMember =>
                    string.Equals(
                        namespaceMember.Name,
                        namespaceSegment,
                        StringComparison.Ordinal
                    )
            );
            if (nextNamespace is null)
            {
                return null;
            }

            currentNamespace = nextNamespace;
        }

        return currentNamespace;
    }

    private static IEnumerable<ServiceRegistration> BuildDirectRegistrations(
        Compilation compilation,
        INamedTypeSymbol symbol,
        InjectableMetadata injectableMetadata,
        IList<OpenGenericBypassWarning> warnings
    )
    {
        var implementationType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var constructor = FindBestPublicConstructor(symbol);
        var serviceTypes = GetServiceTypes(
            compilation,
            symbol,
            implementationType,
            injectableMetadata.ExplicitServiceTypeSymbol,
            injectableMetadata.ExplicitServiceType,
            warnings
        );
        var factoryBody = BuildFactoryBody(symbol, implementationType, constructor, null, null);
        var environmentName = GetConditionalEnvironmentName(symbol);
        var moduleName = injectableMetadata.ModuleName ?? GetModuleName(symbol);

        return serviceTypes.Select(serviceType => new ServiceRegistration(
            serviceType.ServiceType,
            implementationType,
            ResolveRegistrationLifetime(injectableMetadata.Lifetime, serviceType.FallbackLifetime),
            ResolveThreadIsolationLifetime(
                injectableMetadata.ThreadIsolationLifetime,
                serviceType.FallbackThreadIsolationLifetime
            ),
            factoryBody,
            injectableMetadata.Order,
            injectableMetadata.Group,
            injectableMetadata.KeyExpression,
            environmentName,
            moduleName
        ));
    }

    private static IEnumerable<ServiceRegistration> BuildIndirectRegistrations(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> concreteTypes,
        IDictionary<INamedTypeSymbol, InjectableMetadata> injectableTypes,
        IReadOnlyCollection<ServiceRegistration> existingRegistrations,
        IList<OpenGenericBypassWarning> warnings
    )
    {
        var registrations = new List<ServiceRegistration>();
        var existingKeys = new HashSet<string>(
            existingRegistrations.Select(BuildRegistrationIdentity),
            StringComparer.Ordinal
        );
        var injectRequests = injectableTypes
            .SelectMany(pair =>
                GetInjectContractRequests(
                    compilation,
                    pair.Key,
                    pair.Value.ModuleName ?? GetModuleName(pair.Key)
                )
            )
            .ToImmutableArray();

        foreach (var injectRequest in injectRequests)
        {
            if (!IsClosedType(injectRequest.ContractSymbol))
            {
                warnings.Add(
                    BuildOpenGenericBypassWarning(
                        injectRequest.ContractSymbol,
                        "Indirect [Inject] contract discovery"
                    )
                );
                continue;
            }

            if (
                TryBuildOptionsRegistration(
                    injectRequest,
                    existingKeys,
                    out var optionsRegistration
                )
            )
            {
                registrations.Add(optionsRegistration);
                existingKeys.Add(BuildRegistrationIdentity(optionsRegistration));
                continue;
            }

            var registrationIdentity = BuildRegistrationIdentity(
                injectRequest.ServiceType,
                injectRequest.KeyExpression,
                environmentName: null,
                moduleName: injectRequest.ModuleName
            );
            if (existingKeys.Contains(registrationIdentity))
            {
                continue;
            }

            var contractFallbackLifetime = TryGetServiceInjectionLifetime(
                injectRequest.ContractSymbol
            );
            var contractFallbackThreadIsolation = TryGetServiceInjectionThreadIsolationLifetime(
                injectRequest.ContractSymbol
            );
            var bestCandidate = FindIndirectImplementationCandidate(
                compilation,
                injectRequest.ContractSymbol,
                injectRequest.ServiceType,
                concreteTypes,
                injectableTypes,
                contractFallbackLifetime,
                contractFallbackThreadIsolation
            );

            if (bestCandidate is null)
            {
                continue;
            }

            var constructor = FindBestPublicConstructor(bestCandidate.Symbol);
            var factoryBody = BuildFactoryBody(
                bestCandidate.Symbol,
                bestCandidate.ImplementationType,
                constructor,
                null,
                null
            );
            var finalLifetime = injectRequest.LifetimeOverride ?? bestCandidate.Lifetime;
            var environmentName = GetConditionalEnvironmentName(bestCandidate.Symbol);

            registrations.Add(
                new ServiceRegistration(
                    injectRequest.ServiceType,
                    bestCandidate.ImplementationType,
                    finalLifetime,
                    bestCandidate.ThreadIsolationLifetime,
                    factoryBody,
                    bestCandidate.Order,
                    bestCandidate.Group,
                    injectRequest.KeyExpression,
                    environmentName,
                    injectRequest.ModuleName ?? bestCandidate.ModuleName
                )
            );
            existingKeys.Add(registrationIdentity);
        }

        return registrations;
    }

    private static void ApplyDecorators(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> concreteTypes,
        IList<ServiceRegistration> registrations,
        IList<OpenGenericBypassWarning> warnings
    )
    {
        var decorators = concreteTypes
            .SelectMany(typeSymbol =>
                GetDecoratorTargets(compilation, typeSymbol)
                    .Select(target => (Symbol: typeSymbol, Target: target))
            )
            .OrderBy(static decorator => decorator.Target.Order)
            .ThenBy(
                static decorator =>
                    decorator.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                StringComparer.Ordinal
            )
            .ToImmutableArray();

        foreach (var decorator in decorators)
        {
            if (!IsClosedType(decorator.Symbol))
            {
                warnings.Add(
                    BuildOpenGenericBypassWarning(decorator.Symbol, "Decorator registration")
                );
                continue;
            }

            var implementationType = decorator.Symbol.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            var constructor = FindBestPublicConstructor(decorator.Symbol);

            for (var i = registrations.Count - 1; i >= 0; i--)
            {
                var existingRegistration = registrations[i];
                if (
                    existingRegistration.ServiceType != decorator.Target.DisplayName
                    || !string.IsNullOrWhiteSpace(existingRegistration.KeyExpression)
                )
                {
                    continue;
                }

                var factoryBody = BuildFactoryBody(
                    decorator.Symbol,
                    implementationType,
                    constructor,
                    decorator.Target.DisplayName,
                    existingRegistration.FactoryBody
                );
                registrations[i] = new ServiceRegistration(
                    existingRegistration.ServiceType,
                    implementationType,
                    existingRegistration.Lifetime,
                    existingRegistration.ThreadIsolationLifetime,
                    factoryBody,
                    existingRegistration.Order,
                    existingRegistration.Group,
                    existingRegistration.KeyExpression,
                    existingRegistration.EnvironmentName,
                    existingRegistration.ModuleName
                );
                break;
            }
        }
    }

    private static IEnumerable<ServiceRegistration> BuildFactoryRegistrations(
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> concreteTypes,
        IList<OpenGenericBypassWarning> warnings
    )
    {
        var registrations = new List<ServiceRegistration>();
        foreach (var concreteType in concreteTypes)
        {
            foreach (
                var method in concreteType
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(method => IsMethodAccessibleFromGeneratedCode(method, compilation))
            )
            {
                if (!TryGetInjectableFactoryAttribute(method, out var factoryMetadata))
                {
                    continue;
                }

                if (
                    method.IsGenericMethod
                    || !IsClosedType(method.ContainingType)
                    || method.ReturnType is INamedTypeSymbol namedReturnType
                        && !IsClosedType(namedReturnType)
                    || method.Parameters.Any(static parameter =>
                        parameter.Type is INamedTypeSymbol namedParameterType
                        && !IsClosedType(namedParameterType)
                    )
                    || factoryMetadata.HasOpenGenericServiceType
                )
                {
                    warnings.Add(
                        BuildOpenGenericBypassWarning(method, "InjectableFactory registration")
                    );
                    continue;
                }

                var registrationServiceTypeSymbol =
                    factoryMetadata.ServiceTypeSymbol ?? method.ReturnType;
                if (
                    !IsTypeAccessibleFromGeneratedCode(registrationServiceTypeSymbol, compilation)
                    || !IsTypeAccessibleFromGeneratedCode(method.ReturnType, compilation)
                    || method.Parameters.Any(parameter =>
                        !IsTypeAccessibleFromGeneratedCode(parameter.Type, compilation)
                    )
                )
                {
                    continue;
                }

                var serviceType =
                    factoryMetadata.ServiceType
                    ?? method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var factoryCall =
                    $"{method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{method.Name}({BuildMethodParameters(method)})";

                registrations.Add(
                    new ServiceRegistration(
                        serviceType,
                        method.ContainingType.ToDisplayString(
                            SymbolDisplayFormat.FullyQualifiedFormat
                        ),
                        factoryMetadata.Lifetime,
                        factoryMetadata.ThreadIsolationLifetime,
                        factoryCall,
                        factoryMetadata.Order,
                        factoryMetadata.Group,
                        factoryMetadata.KeyExpression,
                        GetConditionalEnvironmentName(method.ContainingType),
                        factoryMetadata.ModuleName ?? GetModuleName(method.ContainingType)
                    )
                );
            }
        }

        return registrations;
    }

    private static string BuildMethodParameters(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            method.Parameters.Select(static parameter =>
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

#pragma warning disable S3776 // registration extraction logic is intentionally centralized
    private static bool TryGetInjectableFactoryAttribute(
        IMethodSymbol method,
        out InjectableFactoryMetadata metadata
    )
    {
        metadata = new InjectableFactoryMetadata(
            lifetime: TransientLifetimeExpression,
            serviceType: null,
            serviceTypeSymbol: null,
            hasOpenGenericServiceType: false,
            order: DefaultOrderingValue,
            group: DefaultOrderingValue,
            keyExpression: null,
            threadIsolationLifetime: null,
            moduleName: null
        );

        foreach (var attributeData in method.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (
                attributeClass is null
                || attributeClass.OriginalDefinition.ToDisplayString()
                    is not (
                        "GenDI.InjectableFactoryAttribute"
                        or "GenDI.InjectableFactoryAttribute<TService>"
                    )
            )
            {
                continue;
            }

            var lifetime = TransientLifetimeExpression;
            var serviceType = default(string);
            var serviceTypeSymbol = default(ITypeSymbol);
            var hasOpenGenericServiceType = false;
            var order = DefaultOrderingValue;
            var group = DefaultOrderingValue;
            var keyExpression = default(string);
            var threadIsolationLifetime = default(string);
            var moduleName = default(string);

            if (
                attributeClass.Arity == 1
                && attributeClass.TypeArguments[0] is ITypeSymbol explicitServiceTypeSymbol
            )
            {
                serviceType = explicitServiceTypeSymbol.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
                serviceTypeSymbol = explicitServiceTypeSymbol;
                if (explicitServiceTypeSymbol is INamedTypeSymbol namedExplicitServiceType)
                {
                    hasOpenGenericServiceType = !IsClosedType(namedExplicitServiceType);
                }
            }

            if (attributeData.ConstructorArguments.Length > 0)
            {
                var first = attributeData.ConstructorArguments[0];
                if (
                    first.Kind == TypedConstantKind.Type
                    && first.Value is ITypeSymbol firstTypeSymbol
                )
                {
                    serviceType = firstTypeSymbol.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );
                    serviceTypeSymbol = firstTypeSymbol;
                    if (firstTypeSymbol is INamedTypeSymbol namedFirstTypeSymbol)
                    {
                        hasOpenGenericServiceType = !IsClosedType(namedFirstTypeSymbol);
                    }
                }
                else
                {
                    lifetime = ConvertLifetimeEnumToExpression(first);
                }
            }

            if (attributeData.ConstructorArguments.Length > 1)
            {
                lifetime = ConvertLifetimeEnumToExpression(attributeData.ConstructorArguments[1]);
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                ApplyCommonRegistrationNamedArgument(
                    namedArgument,
                    ref order,
                    ref group,
                    ref keyExpression,
                    ref threadIsolationLifetime,
                    ref moduleName
                );

                if (
                    namedArgument.Key == "ServiceType"
                    && namedArgument.Value.Kind == TypedConstantKind.Type
                    && namedArgument.Value.Value is INamedTypeSymbol namedServiceType
                )
                {
                    serviceType = namedServiceType.ToDisplayString(
                        SymbolDisplayFormat.FullyQualifiedFormat
                    );
                    serviceTypeSymbol = namedServiceType;
                    hasOpenGenericServiceType = !IsClosedType(namedServiceType);
                }
            }

            metadata = new InjectableFactoryMetadata(
                lifetime,
                serviceType,
                serviceTypeSymbol,
                hasOpenGenericServiceType,
                order,
                group,
                keyExpression,
                threadIsolationLifetime,
                moduleName
            );
            return true;
        }

        return false;
    }
#pragma warning restore S3776

    private static bool TryBuildOptionsRegistration(
        InjectContractRequest injectRequest,
        HashSet<string> existingKeys,
        out ServiceRegistration registration
    )
    {
        registration = default!;
        if (
            !IsIOptionsContract(injectRequest.ContractSymbol, out var optionsType)
            || !TryGetOptionConfigPath(optionsType, out var configPath)
        )
        {
            return false;
        }

        var optionsContractType = injectRequest.ContractSymbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );
        var identity = BuildRegistrationIdentity(
            optionsContractType,
            injectRequest.KeyExpression,
            environmentName: null,
            moduleName: injectRequest.ModuleName
        );
        if (existingKeys.Contains(identity))
        {
            return false;
        }

        var optionsTypeDisplay = optionsType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat
        );
        var escapedPath = EscapeStringLiteral(configPath);
        var escapedTypeName = EscapeStringLiteral(optionsTypeDisplay);
        var factoryBody =
            $"global::Microsoft.Extensions.Options.Options.Create(global::Microsoft.Extensions.Configuration.ConfigurationBinder.Get<{optionsTypeDisplay}>(serviceProvider.GetRequiredService<global::Microsoft.Extensions.Configuration.IConfiguration>().GetSection(\"{escapedPath}\")) ?? throw new global::System.InvalidOperationException(\"Configuration section '{escapedPath}' for options type '{escapedTypeName}' returned null.\"))";

        registration = new ServiceRegistration(
            optionsContractType,
            optionsTypeDisplay,
            injectRequest.LifetimeOverride ?? SingletonLifetimeExpression,
            threadIsolationLifetime: null,
            factoryBody,
            order: DefaultOrderingValue,
            group: DefaultOrderingValue,
            keyExpression: injectRequest.KeyExpression,
            environmentName: null,
            moduleName: injectRequest.ModuleName
        );
        return true;
    }

    private static string BuildRegistrationIdentity(ServiceRegistration registration)
    {
        return BuildRegistrationIdentity(
            registration.ServiceType,
            registration.KeyExpression,
            registration.EnvironmentName,
            registration.ModuleName
        );
    }

    private static string BuildRegistrationIdentity(
        string serviceType,
        string? keyExpression,
        string? environmentName,
        string? moduleName
    )
    {
        return $"{serviceType}|{keyExpression ?? string.Empty}|{environmentName ?? string.Empty}|{moduleName ?? string.Empty}";
    }

#pragma warning disable S3776 // registration extraction logic is intentionally centralized
    private static bool TryGetInjectableAttribute(
        INamedTypeSymbol symbol,
        out InjectableMetadata injectableMetadata
    )
    {
        injectableMetadata = new InjectableMetadata(
            TransientLifetimeExpression,
            explicitServiceType: null,
            explicitServiceTypeSymbol: null,
            hasOpenGenericExplicitServiceType: false,
            order: DefaultOrderingValue,
            group: DefaultOrderingValue,
            keyExpression: null,
            threadIsolationLifetime: null,
            moduleName: null
        );

        foreach (var attributeData in symbol.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is null || !IsInjectableAttribute(attributeClass))
            {
                continue;
            }

            var lifetime = TransientLifetimeExpression;
            var explicitServiceType = default(string);
            var explicitServiceTypeSymbol = default(ITypeSymbol);
            var hasOpenGenericExplicitServiceType = false;
            var order = DefaultOrderingValue;
            var group = DefaultOrderingValue;
            var keyExpression = default(string);
            var threadIsolationLifetime = default(string);
            var moduleName = default(string);

            if (attributeData.ConstructorArguments.Length > 0)
            {
                lifetime = ConvertLifetimeEnumToExpression(attributeData.ConstructorArguments[0]);
            }

            if (
                attributeClass.Arity == 1
                && attributeClass.TypeArguments[0] is ITypeSymbol serviceTypeSymbol
            )
            {
                explicitServiceTypeSymbol = serviceTypeSymbol;
                if (serviceTypeSymbol is INamedTypeSymbol namedServiceTypeSymbol)
                {
                    hasOpenGenericExplicitServiceType = !IsClosedType(namedServiceTypeSymbol);
                }
                explicitServiceType = serviceTypeSymbol.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                ApplyCommonRegistrationNamedArgument(
                    namedArgument,
                    ref order,
                    ref group,
                    ref keyExpression,
                    ref threadIsolationLifetime,
                    ref moduleName
                );
            }

            injectableMetadata = new InjectableMetadata(
                lifetime,
                explicitServiceType,
                explicitServiceTypeSymbol,
                hasOpenGenericExplicitServiceType,
                order,
                group,
                keyExpression,
                threadIsolationLifetime,
                moduleName ?? GetModuleName(symbol)
            );
            return true;
        }

        return false;
    }
#pragma warning restore S3776

    private static bool HasDecoratorTarget(Compilation compilation, INamedTypeSymbol symbol)
    {
        return GetDecoratorTargets(compilation, symbol).Length > 0;
    }

    private static void ApplyCommonRegistrationNamedArgument(
        KeyValuePair<string, TypedConstant> namedArgument,
        ref int order,
        ref int group,
        ref string? keyExpression,
        ref string? threadIsolationLifetime,
        ref string? moduleName
    )
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
            case "ThreadIsolation":
                threadIsolationLifetime = ConvertThreadIsolationPolicyToLifetimeExpression(
                    namedArgument.Value
                );
                break;
            case "Module" when namedArgument.Value.Value is string moduleValue:
                moduleName = moduleValue;
                break;
        }
    }

    private static IEnumerable<OpenGenericBypassWarning> CollectDecoratorOpenGenericWarnings(
        ImmutableArray<INamedTypeSymbol> concreteTypes
    )
    {
        foreach (var concreteType in concreteTypes)
        {
            var hasInferredDecoratorTarget = false;
            foreach (
                var attributeClass in concreteType
                    .GetAttributes()
                    .Where(static attributeData => attributeData.AttributeClass is not null)
                    .Select(static attributeData => attributeData.AttributeClass!)
            )
            {
                if (attributeClass.ToDisplayString() == "GenDI.DecoratorForAttribute")
                {
                    hasInferredDecoratorTarget = true;
                }

                if (
                    attributeClass.OriginalDefinition.ToDisplayString()
                        != "GenDI.DecoratorForAttribute<TService>"
                    || attributeClass.TypeArguments.Length != 1
                    || attributeClass.TypeArguments[0] is not INamedTypeSymbol serviceType
                    || IsClosedType(serviceType)
                )
                {
                    continue;
                }

                yield return BuildOpenGenericBypassWarning(
                    concreteType,
                    "Decorator target contract discovery"
                );
            }

            if (!hasInferredDecoratorTarget)
            {
                continue;
            }

            var inferredContracts = GetAllServiceInjectionContracts(concreteType);
            if (
                inferredContracts.Any()
                && inferredContracts.All(static contract => !IsClosedType(contract))
            )
            {
                yield return BuildOpenGenericBypassWarning(
                    concreteType,
                    "Decorator target contract discovery"
                );
            }
        }
    }

    private static ImmutableArray<DecoratorTarget> GetDecoratorTargets(
        Compilation compilation,
        INamedTypeSymbol symbol
    )
    {
        var targets = ImmutableArray.CreateBuilder<DecoratorTarget>();
        foreach (var attributeData in symbol.GetAttributes())
        {
            var attributeClass = attributeData.AttributeClass;
            if (
                attributeClass?.OriginalDefinition.ToDisplayString()
                == "GenDI.DecoratorForAttribute<TService>"
            )
            {
                if (
                    attributeClass.TypeArguments.Length != 1
                    || attributeClass.TypeArguments[0] is not INamedTypeSymbol serviceType
                    || !IsClosedType(serviceType)
                    || !IsTypeAccessibleFromGeneratedCode(serviceType, compilation)
                )
                {
                    continue;
                }

                targets.Add(
                    new DecoratorTarget(
                        serviceType,
                        serviceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        GetDecoratorOrder(symbol, attributeData)
                    )
                );
                continue;
            }

            if (attributeClass?.ToDisplayString() != "GenDI.DecoratorForAttribute")
            {
                continue;
            }

            var inferredTargets = GetClosedServiceInjectionContracts(compilation, symbol);
            if (inferredTargets.Length != 1)
            {
                continue;
            }

            var inferredTarget = inferredTargets[0];
            targets.Add(
                new DecoratorTarget(
                    inferredTarget,
                    inferredTarget.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    GetDecoratorOrder(symbol, attributeData)
                )
            );
        }

        return targets
            .GroupBy(static target => (target.DisplayName, target.Order))
            .Select(static group => group.First())
            .ToImmutableArray();
    }

#pragma warning disable S3776 // contract resolution intentionally handles multiple precedence branches
    private static ImmutableArray<ServiceContractTarget> GetServiceTypes(
        Compilation compilation,
        INamedTypeSymbol symbol,
        string implementationType,
        ITypeSymbol? explicitServiceTypeSymbol,
        string? explicitServiceType,
        IList<OpenGenericBypassWarning> warnings
    )
    {
        var serviceTypes = new List<ServiceContractTarget>();
        var hasAnyContract = false;

        if (explicitServiceTypeSymbol is not null)
        {
            hasAnyContract = true;
            if (
                !string.IsNullOrWhiteSpace(explicitServiceType)
                && IsTypeAccessibleFromGeneratedCode(explicitServiceTypeSymbol, compilation)
            )
            {
                serviceTypes.Add(new ServiceContractTarget(explicitServiceType, null, null));
            }
        }

        foreach (var interfaceSymbol in symbol.AllInterfaces)
        {
            if (!HasServiceInjectionAttribute(interfaceSymbol))
            {
                continue;
            }

            if (!IsClosedType(interfaceSymbol))
            {
                warnings.Add(
                    BuildOpenGenericBypassWarning(
                        interfaceSymbol,
                        "ServiceInjection interface contract discovery"
                    )
                );
                continue;
            }

            if (!IsTypeAccessibleFromGeneratedCode(interfaceSymbol, compilation))
            {
                continue;
            }

            hasAnyContract = true;
            serviceTypes.Add(
                new ServiceContractTarget(
                    interfaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    TryGetServiceInjectionLifetime(interfaceSymbol),
                    TryGetServiceInjectionThreadIsolationLifetime(interfaceSymbol)
                )
            );
        }

        var baseType = symbol.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (HasServiceInjectionAttribute(baseType))
            {
                if (!IsClosedType(baseType))
                {
                    warnings.Add(
                        BuildOpenGenericBypassWarning(
                            baseType,
                            "ServiceInjection base contract discovery"
                        )
                    );
                    baseType = baseType.BaseType;
                    continue;
                }

                if (!IsTypeAccessibleFromGeneratedCode(baseType, compilation))
                {
                    baseType = baseType.BaseType;
                    continue;
                }

                hasAnyContract = true;
                serviceTypes.Add(
                    new ServiceContractTarget(
                        baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        TryGetServiceInjectionLifetime(baseType),
                        TryGetServiceInjectionThreadIsolationLifetime(baseType)
                    )
                );
            }

            baseType = baseType.BaseType;
        }

        if (!hasAnyContract)
        {
            serviceTypes.Add(new ServiceContractTarget(implementationType, null, null));
        }

        return serviceTypes
            .GroupBy(static target => target.ServiceType, StringComparer.Ordinal)
            .Select(static group =>
                group.FirstOrDefault(static target =>
                    !string.IsNullOrWhiteSpace(target.FallbackLifetime)
                    || !string.IsNullOrWhiteSpace(target.FallbackThreadIsolationLifetime)
                ) ?? group.First()
            )
            .ToImmutableArray();
    }
#pragma warning restore S3776

    private static bool HasServiceInjectionAttribute(ITypeSymbol symbol)
    {
        return symbol
            .GetAttributes()
            .Any(attributeData =>
                attributeData.AttributeClass?.ToDisplayString() == "GenDI.ServiceInjectionAttribute"
            );
    }

    private static ImmutableArray<INamedTypeSymbol> GetAllServiceInjectionContracts(
        INamedTypeSymbol symbol
    )
    {
        var serviceTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        foreach (var interfaceSymbol in symbol.AllInterfaces.Where(HasServiceInjectionAttribute))
        {
            serviceTypes.Add(interfaceSymbol);
        }

        var baseType = symbol.BaseType;
        while (baseType is not null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (HasServiceInjectionAttribute(baseType))
            {
                serviceTypes.Add(baseType);
            }

            baseType = baseType.BaseType;
        }

        return serviceTypes
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<INamedTypeSymbol>()
            .ToImmutableArray();
    }

    private static ImmutableArray<INamedTypeSymbol> GetClosedServiceInjectionContracts(
        Compilation compilation,
        INamedTypeSymbol symbol
    )
    {
        return GetAllServiceInjectionContracts(symbol)
            .Where(contract =>
                IsClosedType(contract) && IsTypeAccessibleFromGeneratedCode(contract, compilation)
            )
            .ToImmutableArray();
    }

    private static int GetDecoratorOrder(INamedTypeSymbol symbol, AttributeData attributeData)
    {
        foreach (var namedArgument in attributeData.NamedArguments)
        {
            if (namedArgument.Key == "Order" && namedArgument.Value.Value is int orderValue)
            {
                return orderValue;
            }
        }

        return TryGetInjectableAttribute(symbol, out var injectableMetadata)
            ? injectableMetadata.Order
            : DefaultOrderingValue;
    }

    private static string ResolveRegistrationLifetime(
        string injectableLifetime,
        string? fallbackLifetime
    )
    {
        if (injectableLifetime != TransientLifetimeExpression)
        {
            return injectableLifetime;
        }

        return string.IsNullOrWhiteSpace(fallbackLifetime) ? injectableLifetime : fallbackLifetime;
    }

    private static string? ResolveThreadIsolationLifetime(
        string? injectableThreadIsolationLifetime,
        string? fallbackThreadIsolationLifetime
    )
    {
        return string.IsNullOrWhiteSpace(injectableThreadIsolationLifetime)
            ? fallbackThreadIsolationLifetime
            : injectableThreadIsolationLifetime;
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

            return TransientLifetimeExpression;
        }

        return null;
    }

    private static string? TryGetServiceInjectionThreadIsolationLifetime(ITypeSymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (
                attributeData.AttributeClass?.ToDisplayString() != "GenDI.ServiceInjectionAttribute"
            )
            {
                continue;
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "ThreadIsolation")
                {
                    return ConvertThreadIsolationPolicyToLifetimeExpression(namedArgument.Value);
                }
            }

            return null;
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

    private static string? GetModuleName(INamedTypeSymbol symbol)
    {
        foreach (var attributeData in symbol.GetAttributes())
        {
            if (
                attributeData.AttributeClass?.ToDisplayString() != "GenDI.InjectableModuleAttribute"
            )
            {
                continue;
            }

            if (
                attributeData.ConstructorArguments.Length > 0
                && attributeData.ConstructorArguments[0].Value is string moduleName
                && !string.IsNullOrWhiteSpace(moduleName)
            )
            {
                return moduleName;
            }
        }

        return null;
    }

    private static OpenGenericBypassWarning BuildOpenGenericBypassWarning(
        INamedTypeSymbol symbol,
        string context
    )
    {
        var location =
            symbol.Locations.FirstOrDefault(static loc => loc.IsInSource) ?? Location.None;
        return new OpenGenericBypassWarning(
            location,
            context,
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );
    }

    private static OpenGenericBypassWarning BuildOpenGenericBypassWarning(
        IMethodSymbol symbol,
        string context
    )
    {
        var location =
            symbol.Locations.FirstOrDefault(static loc => loc.IsInSource) ?? Location.None;
        return new OpenGenericBypassWarning(
            location,
            context,
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );
    }

    private static string BuildFactoryBody(
        INamedTypeSymbol symbol,
        string implementationType,
        IMethodSymbol? constructor,
        string? decoratedServiceType,
        string? decoratedFactoryBody
    )
    {
        var parameters = BuildConstructorParameters(
            constructor,
            decoratedServiceType,
            decoratedFactoryBody
        );
        var injectableProperties = GetInjectableProperties(symbol);

        if (injectableProperties.Length == 0)
        {
            return $"new {implementationType}({parameters})";
        }

        var initializers = string.Join(
            "\n",
            injectableProperties.Select(property =>
            {
                var specialResolution = TryBuildDecoratorResolution(
                    property.TypeSymbol,
                    decoratedServiceType,
                    decoratedFactoryBody
                );
                var resolution =
                    specialResolution
                    ?? BuildResolutionExpression(
                        property.Type,
                        property.KeyExpression,
                        property.UseOptionalResolution
                    );
                return $"                @{property.Name} = {resolution},";
            })
        );

        return $"new {implementationType}({parameters})\n            {{\n{initializers}\n            }}";
    }

    private static string BuildConstructorParameters(
        IMethodSymbol? constructor,
        string? decoratedServiceType,
        string? decoratedFactoryBody
    )
    {
        if (constructor is null || constructor.Parameters.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(
            ", ",
            constructor.Parameters.Select(parameter =>
            {
                var specialResolution = TryBuildDecoratorResolution(
                    parameter.Type,
                    decoratedServiceType,
                    decoratedFactoryBody
                );
                if (!string.IsNullOrWhiteSpace(specialResolution))
                {
                    return specialResolution;
                }

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

    private static string? TryBuildDecoratorResolution(
        ITypeSymbol typeSymbol,
        string? decoratedServiceType,
        string? decoratedFactoryBody
    )
    {
        if (
            string.IsNullOrWhiteSpace(decoratedServiceType)
            || string.IsNullOrWhiteSpace(decoratedFactoryBody)
        )
        {
            return null;
        }

        var typeDisplay = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!string.Equals(typeDisplay, decoratedServiceType, StringComparison.Ordinal))
        {
            return null;
        }

        return $"({decoratedFactoryBody})";
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
                    property.Type,
                    keyExpression,
                    injectMetadata.HasInjectOptionalAttribute
                        || ShouldUseOptionalResolution(property.Type),
                    injectMetadata.HasInjectAttribute,
                    injectMetadata.LifetimeExpression
                );
            })
            .ToImmutableArray();
    }

    private static ImmutableArray<InjectContractRequest> GetInjectContractRequests(
        Compilation compilation,
        INamedTypeSymbol symbol,
        string? moduleName
    )
    {
        return GetInjectableProperties(symbol)
            .Where(static property => property.HasInjectAttribute)
            .Where(static property => property.TypeSymbol is INamedTypeSymbol)
            .Where(property => IsTypeAccessibleFromGeneratedCode(property.TypeSymbol, compilation))
            .Select(static property => new InjectContractRequest(
                (INamedTypeSymbol)property.TypeSymbol,
                property.Type,
                property.KeyExpression,
                property.LifetimeExpression,
                null
            ))
            .Select(request => new InjectContractRequest(
                request.ContractSymbol,
                request.ServiceType,
                request.KeyExpression,
                request.LifetimeOverride,
                moduleName
            ))
            .GroupBy(
                static request => $"{request.ServiceType}|{request.KeyExpression ?? string.Empty}",
                StringComparer.Ordinal
            )
            .Select(static group => group.Last())
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
        var enumValue = Convert.ToInt32(argument.Value ?? 2, CultureInfo.InvariantCulture);

        return enumValue switch
        {
            0 => SingletonLifetimeExpression,
            1 => "ServiceLifetime.Scoped",
            _ => TransientLifetimeExpression,
        };
    }

    private static string? ConvertThreadIsolationPolicyToLifetimeExpression(TypedConstant argument)
    {
        var enumValue = Convert.ToInt32(argument.Value ?? -1, CultureInfo.InvariantCulture);

        if (enumValue < 0)
        {
            return null;
        }

        return enumValue switch
        {
            0 => SingletonLifetimeExpression,
            1 => "ServiceLifetime.Scoped",
            _ => TransientLifetimeExpression,
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

    /// <summary>
    /// Returns <see langword="true"/> when the type symbol allows a <see langword="null"/>
    /// resolved value — either because it is explicitly nullable-annotated or because it
    /// originates from an oblivious (nullable-disabled) context, where assuming non-null
    /// would be unsafe.
    /// </summary>
    private static bool ShouldUseOptionalResolution(ITypeSymbol typeSymbol)
    {
        return typeSymbol.NullableAnnotation
            is NullableAnnotation.Annotated
                or NullableAnnotation.None;
    }

    private static InjectPropertyMetadata GetInjectPropertyMetadata(IPropertySymbol property)
    {
        var keyExpression = default(string);
        var hasInjectOptionalAttribute = false;
        var hasInjectAttribute = false;
        var lifetimeExpression = default(string);

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
            else if (attributeDisplayName == "GenDI.InjectAttribute")
            {
                hasInjectAttribute = true;
                if (attributeData.ConstructorArguments.Length > 0)
                {
                    lifetimeExpression = ConvertLifetimeEnumToExpression(
                        attributeData.ConstructorArguments[0]
                    );
                }
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "Key")
                {
                    keyExpression = BuildTypedConstantExpression(namedArgument.Value);
                }
            }
        }

        return new InjectPropertyMetadata(
            keyExpression,
            hasInjectOptionalAttribute,
            hasInjectAttribute,
            lifetimeExpression
        );
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

    private static bool IsIOptionsContract(
        INamedTypeSymbol contractSymbol,
        out INamedTypeSymbol optionsType
    )
    {
        optionsType = null!;
        if (
            contractSymbol.OriginalDefinition.ToDisplayString()
                != "Microsoft.Extensions.Options.IOptions<TOptions>"
            || contractSymbol.TypeArguments.Length != 1
            || contractSymbol.TypeArguments[0] is not INamedTypeSymbol namedOptionsType
            || !IsClosedType(namedOptionsType)
        )
        {
            return false;
        }

        optionsType = namedOptionsType;
        return true;
    }

    private static bool TryGetOptionConfigPath(INamedTypeSymbol optionsType, out string path)
    {
        foreach (var attributeData in optionsType.GetAttributes())
        {
            if (attributeData.AttributeClass?.ToDisplayString() != "GenDI.OptionConfigAttribute")
            {
                continue;
            }

            if (
                attributeData.ConstructorArguments.Length > 0
                && attributeData.ConstructorArguments[0].Value is string configuredPath
                && !string.IsNullOrWhiteSpace(configuredPath)
            )
            {
                path = configuredPath;
                return true;
            }
        }

        path = string.Empty;
        return false;
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

#pragma warning disable S3776 // indirect candidate resolution intentionally evaluates multiple contract scenarios
    private static ImplementationCandidate? FindIndirectImplementationCandidate(
        Compilation compilation,
        INamedTypeSymbol contractSymbol,
        string contractDisplayName,
        ImmutableArray<INamedTypeSymbol> concreteTypes,
        IDictionary<INamedTypeSymbol, InjectableMetadata> injectableTypes,
        string? contractFallbackLifetime,
        string? contractFallbackThreadIsolationLifetime
    )
    {
        var candidates = new List<ImplementationCandidate>();
        foreach (var concreteType in concreteTypes)
        {
            if (HasDecoratorTarget(compilation, concreteType))
            {
                continue;
            }

            var candidateType = concreteType;
            if (!IsClosedType(candidateType))
            {
                continue;
            }

            if (!ImplementsOrInherits(candidateType, contractSymbol))
            {
                continue;
            }

            InjectableMetadata? injectableMetadata = null;
            if (injectableTypes.TryGetValue(concreteType, out var existingInjectableMetadata))
            {
                injectableMetadata = existingInjectableMetadata;
            }
            else if (TryGetInjectableAttribute(concreteType, out var scannedInjectableMetadata))
            {
                injectableMetadata = scannedInjectableMetadata;
            }

            if (
                injectableMetadata is not null
                && !string.IsNullOrWhiteSpace(injectableMetadata.ExplicitServiceType)
                && !string.Equals(
                    injectableMetadata.ExplicitServiceType,
                    contractDisplayName,
                    StringComparison.Ordinal
                )
            )
            {
                continue;
            }

            var resolvedLifetime = ResolveRegistrationLifetime(
                injectableMetadata?.Lifetime ?? TransientLifetimeExpression,
                contractFallbackLifetime
            );
            var implementationType = candidateType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            candidates.Add(
                new ImplementationCandidate(
                    candidateType,
                    implementationType,
                    resolvedLifetime,
                    ResolveThreadIsolationLifetime(
                        injectableMetadata?.ThreadIsolationLifetime,
                        contractFallbackThreadIsolationLifetime
                    ),
                    injectableMetadata?.Order ?? DefaultOrderingValue,
                    injectableMetadata?.Group ?? DefaultOrderingValue,
                    injectableMetadata?.ModuleName ?? GetModuleName(concreteType)
                )
            );
        }

        return candidates
            .OrderByDescending(static candidate => LifetimePriority(candidate.Lifetime))
            .ThenBy(static candidate => candidate.Group)
            .ThenBy(static candidate => candidate.Order)
            .ThenBy(static candidate => candidate.ImplementationType, StringComparer.Ordinal)
            .FirstOrDefault();
    }
#pragma warning restore S3776

    private static bool ImplementsOrInherits(
        INamedTypeSymbol implementationType,
        INamedTypeSymbol contractType
    )
    {
        if (SymbolEqualityComparer.Default.Equals(implementationType, contractType))
        {
            return true;
        }

        if (contractType.TypeKind == TypeKind.Interface)
        {
            return implementationType.AllInterfaces.Any(interfaceType =>
                SymbolEqualityComparer.Default.Equals(interfaceType, contractType)
            );
        }

        var baseType = implementationType.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, contractType))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool ShouldScanReferencedAssembly(string assemblyName)
    {
        return !(
            assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("xunit", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("testhost", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("coverlet", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
        );
    }

    private static bool IsMethodAccessibleFromGeneratedCode(
        IMethodSymbol method,
        Compilation compilation
    )
    {
        return method is { MethodKind: MethodKind.Ordinary, IsStatic: true }
            && IsDeclaredSymbolAccessibleFromGeneratedCode(method, compilation)
            && IsTypeDeclarationAccessibleFromGeneratedCode(method.ContainingType, compilation);
    }

    private static bool IsTypeDeclarationAccessibleFromGeneratedCode(
        INamedTypeSymbol symbol,
        Compilation compilation
    )
    {
        if (!IsDeclaredSymbolAccessibleFromGeneratedCode(symbol, compilation))
        {
            return false;
        }

        return symbol.ContainingType is null
            || IsTypeDeclarationAccessibleFromGeneratedCode(symbol.ContainingType, compilation);
    }

    private static bool IsTypeAccessibleFromGeneratedCode(
        ITypeSymbol typeSymbol,
        Compilation compilation
    )
    {
        if (typeSymbol.TypeKind == TypeKind.TypeParameter)
        {
            return false;
        }

        return typeSymbol switch
        {
            IArrayTypeSymbol arrayType => IsTypeAccessibleFromGeneratedCode(
                arrayType.ElementType,
                compilation
            ),
            INamedTypeSymbol namedType => IsNamedTypeAccessibleFromGeneratedCode(
                namedType,
                compilation
            ),
            _ => true,
        };
    }

    private static bool IsNamedTypeAccessibleFromGeneratedCode(
        INamedTypeSymbol symbol,
        Compilation compilation
    )
    {
        if (!IsDeclaredSymbolAccessibleFromGeneratedCode(symbol, compilation))
        {
            return false;
        }

        if (
            symbol.ContainingType is not null
            && !IsNamedTypeAccessibleFromGeneratedCode(symbol.ContainingType, compilation)
        )
        {
            return false;
        }

        return symbol.TypeArguments.All(typeArgument =>
            IsTypeAccessibleFromGeneratedCode(typeArgument, compilation)
        );
    }

    private static bool IsDeclaredSymbolAccessibleFromGeneratedCode(
        ISymbol symbol,
        Compilation compilation
    )
    {
        var allowInternal = SymbolEqualityComparer.Default.Equals(
            symbol.ContainingAssembly,
            compilation.Assembly
        );

        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => true,
            Accessibility.Internal => allowInternal,
            _ => false,
        };
    }

    private static bool IsClosedType(INamedTypeSymbol symbol)
    {
        return !symbol.IsUnboundGenericType && symbol.TypeArguments.All(IsClosedTypeArgument);
    }

    private static bool IsClosedTypeArgument(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.TypeKind == TypeKind.TypeParameter)
        {
            return false;
        }

        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return !namedType.IsUnboundGenericType
                && namedType.TypeArguments.All(IsClosedTypeArgument);
        }

        return true;
    }

    /// <summary>
    /// Returns a numeric priority for the given lifetime expression used to break ties
    /// between implementation candidates. Scoped beats Singleton beats Transient
    /// (higher value = higher selection priority).
    /// </summary>
    private static int LifetimePriority(string lifetimeExpression)
    {
        return lifetimeExpression switch
        {
            "ServiceLifetime.Scoped" => 3,
            SingletonLifetimeExpression => 2,
            _ => 1,
        };
    }

    /// <summary>
    /// Selects the public instance constructor with the greatest number of parameters.
    /// Returns <see langword="null"/> when the type has no accessible public constructor.
    /// </summary>
    private static IMethodSymbol? FindBestPublicConstructor(INamedTypeSymbol symbol)
    {
        return symbol
            .InstanceConstructors.Where(static constructorSymbol =>
                constructorSymbol.DeclaredAccessibility == Accessibility.Public
            )
            .OrderByDescending(static constructorSymbol => constructorSymbol.Parameters.Length)
            .FirstOrDefault();
    }

    private sealed class InjectablePropertyInfo
    {
        public InjectablePropertyInfo(
            string name,
            string type,
            ITypeSymbol typeSymbol,
            string? keyExpression,
            bool useOptionalResolution,
            bool hasInjectAttribute,
            string? lifetimeExpression
        )
        {
            Name = name;
            Type = type;
            TypeSymbol = typeSymbol;
            KeyExpression = keyExpression;
            UseOptionalResolution = useOptionalResolution;
            HasInjectAttribute = hasInjectAttribute;
            LifetimeExpression = lifetimeExpression;
        }

        public string Name { get; }

        public string Type { get; }

        public ITypeSymbol TypeSymbol { get; }

        public string? KeyExpression { get; }

        public bool UseOptionalResolution { get; }

        public bool HasInjectAttribute { get; }

        public string? LifetimeExpression { get; }
    }

    private sealed class InjectPropertyMetadata
    {
        public InjectPropertyMetadata(
            string? keyExpression,
            bool hasInjectOptionalAttribute,
            bool hasInjectAttribute,
            string? lifetimeExpression
        )
        {
            KeyExpression = keyExpression;
            HasInjectOptionalAttribute = hasInjectOptionalAttribute;
            HasInjectAttribute = hasInjectAttribute;
            LifetimeExpression = lifetimeExpression;
        }

        public string? KeyExpression { get; }

        public bool HasInjectOptionalAttribute { get; }

        public bool HasInjectAttribute { get; }

        public string? LifetimeExpression { get; }
    }
}

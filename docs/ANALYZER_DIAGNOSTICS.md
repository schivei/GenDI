# GenDI Analyzer Diagnostics

This page is the official reference for `GenDI.Analyzers` diagnostic IDs.

> Packaging note: `GenDI.SourceGenerator` bundles `GenDI.Analyzers` in `analyzers/dotnet/cs`, so consumers installing the source-generator package receive these diagnostics automatically.

## GENDI001 - Inject attribute requires init-only property

- **Category**: `GenDI.Usage`
- **Severity**: Warning
- **Message**: `Property '{0}' uses [Inject] and must declare an init-only setter`
- **When it appears**: A property marked with `[Inject]` is not `init`-only.
- **How to fix**: Change the property to `get; init;` and keep it `public` or `internal`.

## GENDI002 - Injectable attribute requires concrete class

- **Category**: `GenDI.Usage`
- **Severity**: Warning
- **Message**: `Type '{0}' uses [Injectable] and must be a non-abstract class`
- **When it appears**: `[Injectable]` is applied to a non-concrete type (for example abstract class/interface).
- **How to fix**: Apply `[Injectable]` only to concrete, non-abstract classes.

## GENDI003 - Constructor injection can be converted to GenDI property injection

- **Category**: `GenDI.Usage`
- **Severity**: Info
- **Message**: `Constructor injection in '{0}' can be converted to [Inject] init-only properties`
- **When it appears**: A public constructor in an `[Injectable]` class has injectable parameters and no custom logic.
- **How to fix**: Apply the provided code-fix to convert constructor parameters into `[Inject] required ... { get; init; }` properties.

### Practical before/after for GENDI003

Before:

```csharp
[Injectable]
public sealed class CheckoutService
{
    private readonly IPaymentGateway _gateway;

    public CheckoutService(IPaymentGateway gateway)
    {
        _gateway = gateway;
    }
}
```

After code-fix:

```csharp
[Injectable]
public sealed class CheckoutService
{
    [Inject]
    public required IPaymentGateway Gateway { get; init; }
}
```

## GENDI004 - Decorator attribute requires a resolvable service contract

- **Category**: `GenDI.Usage`
- **Severity**: Error
- **Message**: `Decorator '{0}' must declare or infer exactly one closed [ServiceInjection] contract`
- **When it appears**: A non-generic `[DecoratorFor]` type does not implement or inherit exactly one closed `[ServiceInjection]` contract.
- **How to fix**: Either switch to `[DecoratorFor<TService>]` or make the decorator resolve exactly one `[ServiceInjection]` contract in its inheritance/implementation chain.

## GENDI005 - Decorator requires an inner dependency

- **Category**: `GenDI.Usage`
- **Severity**: Error
- **Message**: `Decorator '{0}' must declare a public constructor parameter or [Inject] init-only property of type '{1}'`
- **When it appears**: A decorator does not expose the decorated contract as a public constructor parameter or `[Inject]` init-only property.
- **How to fix**: Add a matching constructor parameter or injectable init-only property so GenDI can compose the pipeline statically.

## IDE links and parity

All diagnostics provide `HelpLinkUri` metadata so IDE warning entries can open the corresponding documentation page quickly. Keep this file and `website/docs/advanced/analyzer-diagnostics.md` synchronized when diagnostics change.

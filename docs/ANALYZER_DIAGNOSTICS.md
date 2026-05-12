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

# Registration Model — RM-08 to RM-12 (Detailed)

This document details the Phase 6 registration-model items delivered in RM-08 through RM-12.

## RM-08 — Dependency scanning across referenced libraries

- GenDI now scans referenced assemblies (excluding framework/test assemblies) for injectable candidates.
- This enables centralized registration even when implementations are defined in referenced solution libraries.
- Behavior is covered by integration tests using `tests/GenDI.ReferenceLibrary`.

## RM-09 — Inferable closed-generic indirect resolution

- Indirect registration via `[Inject]` now supports inferable closed-generic implementations.
- Example: `IGenericRepository<Order>` can resolve from an inferable `GenericRepository<T>` as `GenericRepository<Order>`.
- Open generics are still out-of-scope for registration output and are bypassed when not inferable.

## RM-10 — `OptionConfigAttribute` for `IOptions<>`

- `[OptionConfig("Section:Path")]` marks options types for generated `IOptions<T>` registration.
- Generator emits binding through `IConfiguration.GetSection(path)` + `ConfigurationBinder.Get<T>()`.
- Generated binding throws if section binding resolves to `null` to avoid silent misconfiguration.

## RM-11 — `[InjectableFactory]` on static methods

- Static factory methods can now be used as generated registrations.
- Supports lifetime, key, group/order, module, and thread-isolation metadata.
- Explicit contract can be provided via constructor/type metadata.

## RM-12 — `[InjectableModule]` grouped registration

- Registrations can be grouped by module with `[InjectableModule("...")]` and `Injectable/InjectableFactory` `Module`.
- Generated API now supports:
  - `AddGenDIServices(IServiceCollection services)`
  - `AddGenDIServices(IServiceCollection services, params string[] modules)`
- Passing no modules keeps default behavior (all modules).

## Open-generic guardrails and warnings

- Open-generic candidates are bypassed (not registered) in generator output.
- Generator emits warning `GENDISG001` for skipped open-generic generation paths.
- Applies to injectable classes/contracts/decorators, indirect `[Inject]` contract discovery, and factory registration paths.

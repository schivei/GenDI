---
sidebar_position: 5
---

# RM-08 to RM-12 — Detailed registration model notes

This page details the delivered registration-model features from Phase 6 RM-08 through RM-12.

## RM-08 — Dependency scanning across referenced libraries

- GenDI scans referenced libraries (excluding framework/test assemblies) for injectable types.
- This allows centralized generated registration across solution boundaries.

## RM-09 — Closed-generic indirect inference

- `[Inject]` indirect discovery supports inferable closed-generic implementations.
- Open-generic registrations remain unsupported in generated output.

## RM-10 — `OptionConfigAttribute` + `IOptions<>`

- Mark options with `[OptionConfig("Section:Path")]`.
- GenDI generates `IOptions<T>` binding from `IConfiguration`.

## RM-11 — `[InjectableFactory]` on static methods

- Static factories can define generated registrations with metadata:
  - lifetime
  - key
  - group/order
  - module
  - thread isolation

## RM-12 — `[InjectableModule]` grouped registrations

- Group registrations by module and load selectively:
  - `AddGenDIServices()`
  - `AddGenDIServices("ModuleA", "ModuleB")`

## Open-generic bypass and warnings

- Open-generic generation paths are bypassed and not registered.
- GenDI emits warning `GENDISG001` when bypassing open-generic candidates.
- Applies to injectable classes/contracts/decorators, indirect contracts, and factory registrations.

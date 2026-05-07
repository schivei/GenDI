---
sidebar_position: 1
---

# Introduction to GenDI

GenDI is an attribute-first dependency injection source generator for .NET. It generates DI registrations and activation code at compile time to reduce runtime reflection and keep applications compatible with NativeAOT and trimming.

## Key features and practical value

- **Developer agility**: attribute-driven registration reduces repetitive setup code and speeds up onboarding.
- **Code quality and clarity**: generated activation uses explicit `new` + `GetRequiredService<T>()`, which is easier to inspect and reason about.
- **Predictable behavior**: deterministic ordering with `Group` and `Order` helps avoid ambiguous pipeline composition.
- **Modern DI scenarios**: supports keyed registrations and keyed resolution through both Microsoft DI attributes and GenDI attributes.
- **Performance-oriented defaults**: compile-time generation avoids runtime scanning overhead.
- **Future-proof deployment path**: NativeAOT/trimming support is available when you need it, while still improving day-to-day development for traditional deployments.

## Why GenDI exists

Traditional runtime scanning patterns are practical, but they add startup cost and can create compatibility risks when an app is published with aggressive trimming or NativeAOT. GenDI shifts this work to compile-time:

- Registration mapping is generated from attributes.
- Constructor and init-property injection are generated as strongly typed C# code.
- Microsoft DI remains the runtime container.

## Core goals

- **Predictable service registration**
- **NativeAOT and trim-friendly behavior**
- **Simple, explicit attribute model**
- **Deterministic ordering support for advanced pipelines**

## Feature summary

- `[Injectable]` and `[Injectable<TService>]` to mark concrete services
- `[ServiceInjection]` to mark interfaces/abstract contracts to be injected
- `[Inject]` for init-only property injection (`get; init;`)
- `[assembly: GenDICoveration(...)]` to control generated extension coverage behavior
- Ordering by `Group`, then `Order`, then service type name (ordinal)

## Typical generation output

GenDI generates an extension method in the consumer assembly namespace:

```csharp
// <AssemblyName>.DependencyInjection
services.AddGenDIServices();
```

Each registration uses generated `new` expressions and `GetRequiredService<T>()`, keeping activation explicit and analyzer-friendly.

## Documentation map

- **Getting Started**: installation and first setup
- **Core Concepts**: attributes, contracts, registration strategy
- **Advanced**: NativeAOT/trimming validation and test strategy
- **Community**: contribution and roadmap references

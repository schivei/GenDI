# 📐 Service Registration Model

GenDI generation strategy is deterministic and explicit.

## 🔍 Contract discovery

For each `[Injectable]` class, GenDI gathers contracts in this order:

1. 🔗 Contracts discovered from implemented interfaces / abstract base types marked with `[ServiceInjection]`
2. 🎯 Explicit contract from `Injectable<TService>` (if used)
3. 🔄 Fallback to concrete class itself when no service contract exists

## ⚙️ Activation strategy

Each registration is emitted with a factory that performs typed activation:

- Constructor parameters resolved through `serviceProvider.GetRequiredService<T>()`
- Init-property injections generated for `[Inject]` properties
- 🚫 No runtime type scanning required

## 📊 Deterministic ordering

Registrations are emitted by:

1. `Group`
2. `Order`
3. Service type name (ordinal)

This supports stable ordering for cross-cutting patterns such as staged pipelines.

## 🔀 Mixed generated/non-generated DI

GenDI is intentionally interoperable with normal Microsoft DI registrations. You can combine:

- ⚡ Generated services (`AddGenDIServices()`)
- ✍️ Manual registrations (`AddSingleton`, `AddScoped`, open generics)

The repository includes integration tests validating both directions:

- generated service depending on manually registered services
- manually registered service depending on generated services

## 🧩 Registration strategy control (Phase 6)

GenDI supports explicit generation policy through:

- `RegistrationMultiplicity` (`Single` / `Multiple`)
- `RegistrationEmissionStrategy` (`Add` / `TryAdd`)

These controls can be declared in `ServiceInjection`, `Injectable`, and `Inject` attributes and are resolved deterministically during generation.

## ⚙️ OptionConfig evolution summary

`[OptionConfig]` now supports optional key fallback (`options type name`) and emits optimized options registration via `AddOptions<T>().BindConfiguration(section)` when compatible.

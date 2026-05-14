---
sidebar_position: 4
---

# 🧭 Analyzer Diagnostics (GenDI.Analyzers)

GenDI ships analyzer diagnostics to guide attribute usage and migration paths.

> `GenDI.SourceGenerator` bundles `GenDI.Analyzers` in `analyzers/dotnet/cs`, so these diagnostics are available automatically when the source-generator package is installed.

## 📋 Official diagnostic list

| Code | Severity | Purpose |
|---|---|---|
| `GENDI001` | Warning | `[Inject]` property must be `init`-only |
| `GENDI002` | Warning | `[Injectable]` must target a concrete class |
| `GENDI003` | Info | Constructor injection can be converted to GenDI property injection |
| `GENDI004` | Error | Non-generic `[DecoratorFor]` must resolve exactly one closed `[ServiceInjection]` contract |
| `GENDI005` | Error | Decorators must expose the decorated contract as a constructor parameter or `[Inject]` property |

For the canonical details (message, trigger, fix), see:

- [Analyzer diagnostics index (repository docs)](https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md)

## 🔗 IDE help links

Each diagnostic now exposes `HelpLinkUri`, so IDEs can open the documentation page directly from the analyzer warning/info entry.

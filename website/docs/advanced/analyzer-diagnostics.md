---
sidebar_position: 4
---

# 🧭 Analyzer Diagnostics (GenDI.Analyzers)

GenDI ships analyzer diagnostics to guide attribute usage and migration paths.

## 📋 Official diagnostic list

| Code | Severity | Purpose |
|---|---|---|
| `GENDI001` | Warning | `[Inject]` property must be `init`-only |
| `GENDI002` | Warning | `[Injectable]` must target a concrete class |
| `GENDI003` | Info | Constructor injection can be converted to GenDI property injection |

For the canonical details (message, trigger, fix), see:

- [Analyzer diagnostics index (repository docs)](https://github.com/schivei/GenDI/blob/main/docs/ANALYZER_DIAGNOSTICS.md)

## 🔗 IDE help links

Each diagnostic now exposes `HelpLinkUri`, so IDEs can open the documentation page directly from the analyzer warning/info entry.

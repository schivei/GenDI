# NativeAOT and Trimming

GenDI is built to avoid runtime reflection-heavy registration paths.

## What is included in this repository

- `src/GenDI/ILLink.xml` descriptor support
- trim publish validation app under `tests/GenDI.Phase3.TrimValidation.App`
- NativeAOT publish validation app under `tests/GenDI.Phase3.NativeAotValidation.App`
- automated validation tests under `tests/GenDI.Phase3.Validation.Tests`

## Why this matters

NativeAOT and trim modes can remove metadata/code paths not statically referenced. GenDI reduces this risk by emitting direct activation code.

## Recommended publish checks

```bash
dotnet publish tests/GenDI.Phase3.TrimValidation.App/GenDI.Phase3.TrimValidation.App.csproj -c Release
dotnet publish tests/GenDI.Phase3.NativeAotValidation.App/GenDI.Phase3.NativeAotValidation.App.csproj -c Release
```

## Best practices

- Prefer explicit contracts via `Injectable<TService>`.
- Keep property injection limited to required init-only dependencies.
- Validate trim and NativeAOT outputs in CI.

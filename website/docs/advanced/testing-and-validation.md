# 🧪 Testing and Validation Strategy

GenDI uses layered validation:

## 1️⃣ Generator behavior tests

Projects:

- `GenDI.SourceGenerator.CoverageEnabled.Tests`
- `GenDI.SourceGenerator.CoverageDisabled.Tests`
- `GenDI.SourceGenerator.NoCoverageAttribute.Tests`

Coverage includes:

- ✅ generated coverage attribute behavior
- ✅ constructor/init-property activation generation
- ✅ ordering rules (`Group`, `Order`, name)
- ✅ contract discovery and fallback behavior

## 2️⃣ Runtime integration tests

Project:

- `GenDI.Integration.Tests`

Covers realistic `IServiceCollection` composition, including open generic manual registrations and mixed generated/non-generated dependencies.

## 3️⃣ Publish compatibility tests

Project:

- `GenDI.Phase3.Validation.Tests`

Executes trim/AOT publish verification paths.

## 4️⃣ Platform/framework validation

Projects:

- `GenDI.Phase6.MinimalApiValidation.App`
- `GenDI.Phase6.WorkerValidation.App`
- `GenDI.Phase6.BlazorWasmValidation.App`
- `GenDI.Phase6.PlatformValidation.Tests`

Covers:

- ✅ Minimal API publish validation
- ✅ Worker Service publish validation
- ✅ Blazor WebAssembly publish validation
- ✅ F# exploration proving generated `AddGenDIServices()` is currently unavailable in `fsproj`

## 5️⃣ Testing ergonomics (`GenDI.Testing`)

Projects:

- `GenDI.Testing.Tests`
- `GenDI.Testing.Example.Tests`

Covers:

- ✅ `ServiceBuilder` fluent helper for test-time `IServiceCollection` composition
- ✅ Integration with DI abstractions descriptor helpers (`TryAdd`, `Replace`, `TryAddEnumerable`)
- ✅ Real-world xUnit usage with generated `AddGenDIServices` and property injection

## 🛠️ Local validation commands

```bash
dotnet build GenDI.slnx
dotnet test GenDI.slnx
dotnet test tests/GenDI.Phase6.PlatformValidation.Tests -c Release
dotnet test tests/GenDI.Testing.Tests
dotnet test tests/GenDI.Testing.Example.Tests
```

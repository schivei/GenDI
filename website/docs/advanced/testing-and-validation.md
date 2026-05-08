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

## 🛠️ Local validation commands

```bash
dotnet build GenDI.slnx
dotnet test GenDI.slnx
```

# 📦 Installation

## 1️⃣ Add runtime package reference

Reference the core package in the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="GenDI" Version="x.y.z" />
</ItemGroup>
```

## 2️⃣ Add generator as analyzer

When consumed as NuGet, the source generator should be wired as analyzer/private assets behavior:

```xml
<ItemGroup>
  <PackageReference Include="GenDI.SourceGenerator" Version="x.y.z"
                    PrivateAssets="all"
                    IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
</ItemGroup>
```

## 3️⃣ Add attributes in your code

```csharp
[ServiceInjection]
public interface IMyService { }

[Injectable<IMyService>(ServiceLifetime.Scoped)]
public sealed class MyService : IMyService
{
}
```

## 4️⃣ Register generated services

```csharp
using <YourAssemblyName>.DependencyInjection;

builder.Host.UseGenDI();

builder.Services.AddGenDIServices();
```

## 5️⃣ Optional: coverage behavior toggle

```csharp
[assembly: GenDI.GenDICoveration(true)]
```

- ✅ `true` (default): generated extension remains included in coverage.
- ⛔ `false`: generated extension receives `[ExcludeFromCodeCoverage]`.

## 6️⃣ Optional: advanced registration policies

GenDI Phase 6 also supports:

- `RegistrationMultiplicity` and `RegistrationEmissionStrategy` for `Single`/`Multiple` and `Add`/`TryAdd` code generation.
- `[OptionConfig]` with optional key fallback (type name) for `IOptions<T>` binding.

## 🛠️ Local tooling and hooks

The repository ships local tools (`csharpier`, `husky`) in `dotnet-tools.json`.

- 🔄 pre-commit formats and validates:
  - `dotnet csharpier format .`
  - `dotnet test`
- 🔧 on fresh clone, the main project runs a pre-restore target that restores local tools and installs husky hooks.

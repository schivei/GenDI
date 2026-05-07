# Installation

## 1) Add runtime package reference

Reference the core package in the consuming project:

```xml
<ItemGroup>
  <PackageReference Include="GenDI" Version="x.y.z" />
</ItemGroup>
```

## 2) Add generator as analyzer

When consumed as NuGet, the source generator should be wired as analyzer/private assets behavior:

```xml
<ItemGroup>
  <PackageReference Include="GenDI.SourceGenerator" Version="x.y.z"
                    PrivateAssets="all"
                    IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
</ItemGroup>
```

## 3) Add attributes in your code

```csharp
[ServiceInjection]
public interface IMyService { }

[Injectable<IMyService>(ServiceLifetime.Scoped)]
public sealed class MyService : IMyService
{
}
```

## 4) Register generated services

```csharp
using <YourAssemblyName>.DependencyInjection;

services.AddGenDIServices();
```

## 5) Optional: coverage behavior toggle

```csharp
[assembly: GenDI.GenDICoveration(true)]
```

- `true` (default): generated extension remains included in coverage.
- `false`: generated extension receives `[ExcludeFromCodeCoverage]`.

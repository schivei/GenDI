# GenDI

> **Generator-based Dependency Injection for NativeAOT**

GenDI is a dependency injection library built on top of C# *source generators*, providing full compatibility with NativeAOT and trimming. It works as an additional module to `Microsoft.Extensions.DependencyInjection`, allowing you to register services automatically at compile time — no reflection required.

---

## Installation

```bash
dotnet add package GenDI
```

---

## Usage

### Using `InjectableAttribute`

```csharp
[Injectable(ServiceLifetime.Singleton, Order = 1, Group = "mygroup")]
public class MeuServico : IMeuServico
{
    public void Executar() => Console.WriteLine("Servico injetado!");
}
```

### Using `I*Injectable`

```csharp
public class MeuServico : IMeuServico, ISingletonInjectable
{
    public void Executar() => Console.WriteLine("Servico injetado!");
}
```

### Registering Services

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGenDIServices();
var app = builder.Build();
app.Run();
```

---

## Compatibility

| Platform / Framework                         | Supported |
|----------------------------------------------|-----------|
| .NET 8+                                      | YES       |
| NativeAOT                                    | YES       |
| Trimming                                     | YES       |
| Microsoft.Extensions.DependencyInjection     | YES       |

---

## Roadmap

| Phase | Description                                               | Status     |
|-------|-----------------------------------------------------------|------------|
| 1     | `InjectableAttribute` - attribute-based registration      | Planned    |
| 2     | `I*Injectable` - interface-based registration             | Planned    |
| 3     | Microsoft DI integration (source-generated extensions)    | Planned    |
| 4     | Advanced NativeAOT support (ILLink.xml, type preservation)| Planned    |
| 5     | Official NuGet publication                                | Planned    |

See the full plan in [ROADMAP.md](ROADMAP.md).

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

---

## License

This project is licensed under the MIT License - see [LICENSE.md](LICENSE.md) for details.

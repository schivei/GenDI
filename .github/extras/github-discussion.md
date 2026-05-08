# 🎉 Introducing GenDI — compile-time dependency injection for .NET

Hey everyone! 👋

I'm excited to share **GenDI**, an open-source C# source generator that makes dependency
injection in .NET cleaner, faster, and AOT-ready — without sacrificing any of the power you
expect from Microsoft DI.

---

## The problem it solves

Every time you add a dependency to a service, you're forced to:

1. Add a constructor parameter
2. Declare a private backing field
3. Write an assignment in the constructor body

For two or three dependencies that's fine. For real-world services with five, six, or more
dependencies it becomes noise — pure maintenance overhead that distracts from actual business
logic.

---

## The GenDI way

```csharp
// Instead of this...
public class OrderProcessor
{
    private readonly IOrderRepository _repo;
    private readonly IPaymentGateway _payment;
    private readonly IEmailService _email;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(
        IOrderRepository repo, IPaymentGateway payment,
        IEmailService email, ILogger<OrderProcessor> logger)
    {
        _repo = repo; _payment = payment;
        _email = email; _logger = logger;
    }
}

// ...write this
[Injectable<IOrderProcessor>(ServiceLifetime.Scoped)]
public class OrderProcessor : IOrderProcessor
{
    [Inject] public required IOrderRepository Repo { get; init; }
    [Inject] public required IPaymentGateway Payment { get; init; }
    [Inject] public required IEmailService Email { get; init; }
    [Inject] public required ILogger<OrderProcessor> Logger { get; init; }
}
```

One `[Inject]` property per dependency. No fields, no constructor, no assignments. GenDI
generates all the wiring code at **compile time**.

---

## Key highlights

- 🔖 **Attribute-first**: mark services with `[Injectable]`, contracts with `[ServiceInjection]`
- ⚡ **~19× faster startup** than reflection-based assembly scanning
- 🏗️ **NativeAOT and trimming compatible** out of the box
- 🔑 **Keyed services** via `[Inject(Key = "...")]` or `[FromKeyedServices]`
- 🧪 **Compile-time safety**: the `required` keyword ensures every dependency is wired
- 📦 **Zero lock-in**: Microsoft DI is still the runtime container — GenDI is purely additive

---

## Benchmarks

| Strategy | Mean startup | Allocated |
|---|---:|---:|
| Manual (no GenDI) | 1.842 μs | 5.21 KB |
| GenDI constructor injection | 2.007 μs | 5.68 KB |
| GenDI property injection | 2.031 μs | 5.71 KB |
| Reflection scanner | 37.901 μs | 14.54 KB |

Property injection and constructor injection are **within noise** — so you can pick the clean
ergonomic style without any performance penalty.

---

## Get started

```bash
dotnet add package GenDI
```

- 📖 Docs: https://elton.schivei.nom.br/GenDI
- 💻 GitHub: https://github.com/schivei/GenDI
- 📦 NuGet: https://www.nuget.org/packages/GenDI

---

## Roadmap

We have a big list of ideas coming — analyzers, `[InjectOptional]`, decorator pattern
auto-wiring, `GenDI.Testing`, Blazor WASM validation, `dotnet new` templates and more.
Check the [full roadmap](https://github.com/schivei/GenDI/blob/main/ROADMAP.md).

---

I'd love your feedback! Have you dealt with constructor bloat in your .NET projects? What
features would make GenDI more useful for you? Drop a comment below 👇

And if you'd like to contribute or sponsor the project:
- 🤝 [Contributing guide](https://elton.schivei.nom.br/GenDI/docs/community/contributing)
- ❤️ [Sponsor on GitHub](https://github.com/sponsors/schivei)

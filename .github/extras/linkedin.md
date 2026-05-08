# 🚀 I built a C# source generator that eliminates constructor injection boilerplate — and it's fully NativeAOT ready

If you've written .NET services with more than three or four dependencies, you know the ritual:

✏️ Add a constructor parameter
📦 Declare a private backing field
🔗 Write the assignment

Repeat for every dependency. Repeat for every service. It's mechanical, it adds noise, and it distracts from the business logic that actually matters.

---

**GenDI** is my answer to this. It's an open-source C# source generator that lets you declare dependencies as `required` init-only properties:

```csharp
[Injectable<IOrderProcessor>(ServiceLifetime.Scoped)]
public class OrderProcessor : IOrderProcessor
{
    [Inject] public required IOrderRepository Repo { get; init; }
    [Inject] public required IPaymentGateway Payment { get; init; }
    [Inject] public required IEmailService Email { get; init; }
    [Inject] public required ILogger<OrderProcessor> Logger { get; init; }
}
```

One attribute. No constructor. No private fields. GenDI generates the wiring code at **compile time** — so there's nothing to debug at runtime.

---

**Here's what makes it worth looking at:**

⚡ ~19× faster startup than reflection-based assembly scanning (benchmarked with BenchmarkDotNet)

🔒 `required` keyword + compiler enforcement = no missing dependencies at runtime

🏗️ Full NativeAOT and IL-trimming support — safe for mobile, embedded, and cloud-native deployments

📊 Property injection vs constructor injection? **Within measurement noise (±1–2 %)** — choose elegance without paying any performance price

🧩 Purely additive — Microsoft DI is still the runtime container, GenDI only generates the wiring

---

**Install:**

```
dotnet add package GenDI
```

📖 Docs: https://elton.schivei.nom.br/GenDI
💻 GitHub: https://github.com/schivei/GenDI

---

I'm actively expanding the roadmap: analyzers, optional injection, decorator pattern auto-wiring, a `GenDI.Testing` companion, Blazor WASM support, and more.

If you're tired of constructor boilerplate in .NET — give it a try and let me know what you think! 💬

And if GenDI saves your team time, consider supporting it:
❤️ https://github.com/sponsors/schivei

#dotnet #csharp #opensource #dependencyinjection #sourcegeneration #nativeaot #softwareengineering

# Attribute Reference

## `InjectableAttribute`

Marks a concrete class for generation-based registration.

```csharp
[Injectable(ServiceLifetime.Transient)]
public sealed class ConcreteService { }
```

### Key members

- `Lifetime`: DI lifetime (`Singleton`, `Scoped`, `Transient`)
- `Group`: primary ordering key (default `int.MaxValue`)
- `Order`: secondary ordering key inside group (default `int.MaxValue`)
- `Key`: optional keyed-service identifier (default `null`)

`ServiceType` in non-generic form is always `null`.

## `InjectableAttribute<TService>`

Use when you want an explicit contract through a closed generic argument.

```csharp
[Injectable<IMyService>(ServiceLifetime.Singleton)]
public sealed class MyService : IMyService { }
```

`ServiceType` is automatically derived as `typeof(TService)`.

## `ServiceInjectionAttribute`

Marks interfaces or abstract types that should be discoverable as injectable contracts while GenDI traverses implementations and inheritance.

```csharp
[ServiceInjection]
public interface IMyService { }
```

## `InjectAttribute`

`[Inject]` is the heart of GenDI's property injection model. Mark any `required` init-only property with it and GenDI generates the resolution code at compile time — no constructor, no private field, no assignment boilerplate.

### Why property injection?

Every constructor parameter forces three lines of code: the parameter itself, a private backing field, and an assignment. As services grow, constructors become the loudest part of a file. Property injection collapses all three into one declarative line:

```csharp
// Without property injection — 3 lines of ceremony per dependency
private readonly IOrderRepository _repo;
public MyService(IOrderRepository repo) { _repo = repo; }

// With [Inject] — 1 declarative line, zero private fields
[Inject] public required IOrderRepository Repo { get; init; }
```

The `required` modifier is enforced by the C# compiler: GenDI cannot generate an instance without providing every `[Inject]` property. This gives you the same compile-time safety as constructor injection, with far less noise.

### Basic usage

```csharp
[Inject]
public required IOtherService OtherService { get; init; }
```

### Keyed property injection

```csharp
[Inject(Key = "primary")]
public required IOtherService OtherService { get; init; }
```

Constructor parameters can use native DI keyed resolution:

```csharp
public Consumer([FromKeyedServices("primary")] IOtherService otherService) { }
```

### Benefits at a glance

| Scenario | Constructor injection | `[Inject]` property injection |
|---|---|---|
| Add dependency | Edit ctor + field + assignment | Add one property |
| Read class structure | Scan ctor + private fields | Properties visible immediately |
| Unit test partial wiring | Must satisfy full ctor | Assign only needed properties |
| Avoid parameter order bugs | Position-sensitive | Named — no ordering risk |

### Requirements

- Property must be `get; init;`
- Property must be `public` or `internal`

## `GenDICoverationAttribute`

Assembly-level toggle for generated extension coverage behavior.

```csharp
[assembly: GenDI.GenDICoveration(false)]
```

- `true` => include generated extension in coverage
- `false` => append `[ExcludeFromCodeCoverage]` to generated extension

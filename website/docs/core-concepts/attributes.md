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

Enables generated init-only property injection.

```csharp
[Inject]
public required IOtherService OtherService { get; init; }
```

Requirements:

- Property must be `get; init;`
- Property must be public or internal

## `GenDICoverationAttribute`

Assembly-level toggle for generated extension coverage behavior.

```csharp
[assembly: GenDI.GenDICoveration(false)]
```

- `true` => include generated extension in coverage
- `false` => append `[ExcludeFromCodeCoverage]` to generated extension

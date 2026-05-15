# Platform and Framework Support

This note consolidates the Phase 6 platform/framework validation work for GenDI.

## Validation assets in this repository

- `tests/GenDI.Phase6.MinimalApiValidation.App` — ASP.NET Core Minimal API sample using `AddGenDIServices()`
- `tests/GenDI.Phase6.WorkerValidation.App` — Worker Service / hosted service sample
- `tests/GenDI.Phase6.BlazorWasmValidation.App` — Blazor WebAssembly sample
- `tests/GenDI.Phase6.PlatformValidation.Tests` — automated publish checks for the three projects above plus F# exploration coverage

## Minimal API

Pattern:

1. register generated services on `builder.Services`
2. keep endpoint handlers thin
3. inject GenDI-managed services directly into the route delegate

Example:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();

var app = builder.Build();
app.MapGet("/orders/{id:guid}", (Guid id, IOrderEndpointService orders) =>
    Results.Ok(orders.Create(id)));
```

Validation status: automated publish covered by `Phase6PlatformValidationTests.MinimalApi_publish_succeeds`.

## Worker Service / hosted services

Pattern:

1. call `AddGenDIServices()` before registering the hosted worker
2. let the `BackgroundService` use normal constructor injection
3. keep GenDI property injection inside the services consumed by the worker

Example:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();
builder.Services.AddHostedService<Worker>();
```

Validation status: automated publish covered by `Phase6PlatformValidationTests.WorkerService_publish_succeeds`.

## Blazor WebAssembly

Pattern:

1. register GenDI services in `Program.cs`
2. keep Razor components using standard Blazor `@inject` / `Microsoft.AspNetCore.Components.Inject`
3. use fully-qualified `GenDI.Inject` inside GenDI-managed services to avoid attribute-name ambiguity

Example:

```csharp
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();
```

```razor
@inject IOrderDashboardService Dashboard

<p>@Dashboard.BuildSummary()</p>
```

Validation status: automated publish covered by `Phase6PlatformValidationTests.BlazorWasm_publish_succeeds`.

## MAUI / mobile AOT

Repository CI does not have MAUI workloads installed, so mobile publish cannot be executed automatically here.
The recommended integration path is still the same:

1. keep GenDI-annotated services in a shared project
2. call `builder.Services.AddGenDIServices()` from `MauiProgram.CreateMauiApp()`
3. validate platform publish locally or in workload-enabled CI

Suggested commands after installing MAUI workloads:

```bash
dotnet publish MyMauiApp.csproj -c Release -f net10.0-android
dotnet publish MyMauiApp.csproj -c Release -f net10.0-ios
```

The shared-service AOT-safe behavior is already covered by the Phase 3 trim/NativeAOT validation projects in this repository.

## F# exploration

Exploration result: F# projects can consume GenDI attributes, but they do not receive the generated `AddGenDIServices()` extension method.
This is now documented and verified by `Phase6PlatformValidationTests.FSharp_projects_do_not_receive_generated_AddGenDIServices_extension`.

Current recommendation:

- use GenDI from C# projects
- or keep F# registrations manual until source-generator integration for F# is available

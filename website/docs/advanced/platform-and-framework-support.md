# 🌐 Platform and Framework Support

This page summarizes the platform/framework work delivered for Phase 6.

## Validation assets in this repository

- `tests/GenDI.Phase6.MinimalApiValidation.App`
- `tests/GenDI.Phase6.WorkerValidation.App`
- `tests/GenDI.Phase6.BlazorWasmValidation.App`
- `tests/GenDI.Phase6.PlatformValidation.Tests`

## ASP.NET Core Minimal API

Use GenDI registrations at startup and inject GenDI-managed services directly into endpoint delegates:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();

var app = builder.Build();
app.MapGet("/orders/{id:guid}", (Guid id, IOrderEndpointService orders) =>
    Results.Ok(orders.Create(id)));
```

Validated by automated publish in `Phase6PlatformValidationTests.MinimalApi_publish_succeeds`.

## Worker Service / hosted services

Register generated services before the hosted worker and keep the worker itself on standard constructor injection:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();
builder.Services.AddHostedService<Worker>();
```

Validated by automated publish in `Phase6PlatformValidationTests.WorkerService_publish_succeeds`.

## Blazor WebAssembly

GenDI works well for the services consumed by your components.
Keep the component surface on normal Blazor `@inject`, and keep GenDI property injection inside the service implementation:

```csharp
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();
```

```razor
@inject IOrderDashboardService Dashboard

<p>@Dashboard.BuildSummary()</p>
```

> Tip: in service classes used by Blazor, prefer `GenDI.Inject` explicitly to avoid confusion with `Microsoft.AspNetCore.Components.Inject`.

Validated by automated publish in `Phase6PlatformValidationTests.BlazorWasm_publish_succeeds`.

## MAUI / mobile AOT

This repository does not run MAUI workloads in CI, so mobile publish validation remains a documented manual step.
Recommended setup:

1. place GenDI-annotated services in a shared project
2. call `builder.Services.AddGenDIServices()` from `MauiProgram.CreateMauiApp()`
3. validate Android/iOS publish on a machine or CI image with MAUI workloads installed

Suggested commands:

```bash
dotnet publish MyMauiApp.csproj -c Release -f net10.0-android
dotnet publish MyMauiApp.csproj -c Release -f net10.0-ios
```

The shared AOT-safe registration model is already covered by the Phase 3 trim/NativeAOT validation apps in this repository.

## F# support exploration

Current result: F# projects can reference GenDI attributes, but they do not receive the generated `AddGenDIServices()` extension.
That limitation is verified by `Phase6PlatformValidationTests.FSharp_projects_do_not_receive_generated_AddGenDIServices_extension`.

For now:

- use GenDI from C# projects when you need generated registration
- keep F# service registration manual if the application host is F#

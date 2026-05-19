using GenDI;
using GenDI.Phase6.MinimalApiValidation.App;
using GenDI.Phase6.MinimalApiValidation.App.DependencyInjection;

[assembly: GenDICoveration(false)]

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseGenDI();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet(
    "/orders/{id:guid}",
    (Guid id, IOrderEndpointService orders) => Results.Ok(orders.Create(id))
);

await app.RunAsync();

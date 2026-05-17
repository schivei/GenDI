using GenDI.Phase6.WorkerValidation.App;
using GenDI.Phase6.WorkerValidation.App.DependencyInjection;

[assembly: GenDI.GenDICoveration(false)]

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
await host.RunAsync();

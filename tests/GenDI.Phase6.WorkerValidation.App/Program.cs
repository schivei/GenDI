using GenDI.Phase6.WorkerValidation.App;
using GenDI.Phase6.WorkerValidation.App.DependencyInjection;

[assembly: GenDI.GenDICoveration(false)]

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);

// The [Hosted] attribute on Worker makes GenDI emit the AddHostedService<Worker>
// registration (with property injection) as part of AddGenDIServices().
builder.Services.AddGenDIServices();

var host = builder.Build();
await host.RunAsync();

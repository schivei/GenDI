using GenDI.Phase6.BlazorWasmValidation.App;
using GenDI.Phase6.BlazorWasmValidation.App.DependencyInjection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

[assembly: GenDI.GenDICoveration(false)]

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();

await builder.Build().RunAsync();

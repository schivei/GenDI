using GenDI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GenDI.Phase6.BlazorWasmValidation.App;
using GenDI.Phase6.BlazorWasmValidation.App.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddGenDIServices();

await builder.Build().RunAsync();

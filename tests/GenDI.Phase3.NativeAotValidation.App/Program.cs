using GenDI;
using GenDI.Phase3.NativeAotValidation.App.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddGenDIServices();

var provider = services.BuildServiceProvider();
var service = provider.GetRequiredService<IMyService>();
service.Execute();

[ServiceInjection]
public interface IMyService
{
    void Execute();
}

[Injectable<IMyService>(ServiceLifetime.Singleton)]
public sealed class MyService : IMyService
{
    public void Execute() { }
}

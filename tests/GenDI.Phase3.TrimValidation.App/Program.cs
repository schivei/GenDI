using GenDI.Phase3.TrimValidation.App.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: GenDI.GenDiCoveration(false)]

var services = new ServiceCollection();
services.AddGenDIServices();

var provider = services.BuildServiceProvider();
var service = provider.GetRequiredService<GenDI.Phase3.TrimValidation.App.IMyService>();
service.Execute();

namespace GenDI.Phase3.TrimValidation.App
{
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
}

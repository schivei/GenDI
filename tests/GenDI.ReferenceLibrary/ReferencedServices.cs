using GenDI.ReferenceLibrary.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: GenDI.GenDICoveration(false)]

namespace GenDI.ReferenceLibrary;

[ServiceInjection]
public interface IReferencedContract { }

[Injectable<IReferencedContract>(ServiceLifetime.Singleton, Module = "Referenced")]
public sealed class ReferencedService : IReferencedContract;

public static class ReferencedServiceCollectionExtensions
{
    public static IServiceCollection AddReferencedModule(this IServiceCollection services)
    {
        return services.AddGenDIServices("Referenced");
    }
}

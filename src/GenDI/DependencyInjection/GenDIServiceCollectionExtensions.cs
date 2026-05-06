using Microsoft.Extensions.DependencyInjection;

namespace GenDI.DependencyInjection;

public static class GenDIServiceCollectionExtensions
{
    public static IServiceCollection AddGenDIServices(this IServiceCollection services)
    {
        GenDIRegistration.Register(services);
        return services;
    }
}

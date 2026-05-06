using Microsoft.Extensions.DependencyInjection;

namespace GenDI.DependencyInjection;

internal static partial class GenDIRegistration
{
    public static void Register(IServiceCollection services)
    {
        RegisterGenerated(services);
    }

    static partial void RegisterGenerated(IServiceCollection services);
}

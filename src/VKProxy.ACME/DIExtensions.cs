using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VKProxy.Middlewares.Http;

namespace VKProxy.ACME;

public static class DIExtensions
{
    public static IServiceCollection AddACME(this IServiceCollection services, Action<AcmeOptions> config = null)
    {
        var op = new AcmeOptions();
        config?.Invoke(op);
        services.AddSingleton(op);
        services.TryAddTransient<IAcmeContext, AcmeContext>();
        services.TryAddSingleton<IForwarderHttpClientFactory, ForwarderHttpClientFactory>();
        services.TryAddSingleton<IAcmeHttpClient, DefaultAcmeHttpClient>();
        services.TryAddSingleton<IAcmeClient, AcmeClient>();
        return services;
    }
}
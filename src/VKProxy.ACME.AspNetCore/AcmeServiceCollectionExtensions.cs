using Microsoft.AspNetCore.Server.Kestrel.Https;
using VKProxy.ACME;
using VKProxy.ACME.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class AcmeServiceCollectionExtensions
{
    public static IServiceCollection AddAcmeChallenge(this IServiceCollection services, Action<AcmeChallengeOptions> action, Action<AcmeOptions> config = null)
    {
        var op = new AcmeChallengeOptions();
        action(op);
        services.AddSingleton(op);
        services.AddACME(config);
        return services;
    }

    public static HttpsConnectionAdapterOptions UseAcmeChallenge(
       this HttpsConnectionAdapterOptions httpsOptions,
       IServiceProvider applicationServices)
    {
        throw new NotImplementedException();
    }
}
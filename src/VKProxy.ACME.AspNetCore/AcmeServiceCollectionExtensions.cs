using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Hosting;
using VKProxy.ACME;
using VKProxy.ACME.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class AcmeServiceCollectionExtensions
{
    public static IServiceCollection AddAcmeChallenge(this IServiceCollection services, Action<AcmeChallengeOptions> action, Action<AcmeOptions> config = null)
    {
        var op = new AcmeChallengeOptions();
        action(op);
        op.Check();
        services.AddSingleton(op);
        services.AddACME(config);
        services.AddSingleton<ICertificateSource, DeveloperCertSource>();
        services.AddSingleton<ICertificateSource, X509CertStoreSource>();
        services.AddSingleton<ServerCertificateSelector>();
        services.AddSingleton<IServerCertificateSelector>(i => i.GetRequiredService<ServerCertificateSelector>());
        services.AddSingleton<IHostedService, AcmeLoader>();
        services.AddTransient<IAcmeState, InitAcmeState>();
        services.AddTransient<BeginCertificateCreationAcmeState>();
        services.AddTransient<CheckForRenewalAcmeState>();
        return services;
    }

    public static HttpsConnectionAdapterOptions UseAcmeChallenge(
       this HttpsConnectionAdapterOptions httpsOptions,
       IServiceProvider applicationServices)
    {
        var certificateSelector = applicationServices.GetRequiredService<IServerCertificateSelector>();
        var fallbackSelector = httpsOptions.ServerCertificateSelector;
        httpsOptions.ServerCertificateSelector = (connectionContext, domainName) =>
        {
            var primaryCert = certificateSelector.Select(connectionContext!, domainName);
            return primaryCert ?? fallbackSelector?.Invoke(connectionContext, domainName);
        };

        return httpsOptions;
    }
}
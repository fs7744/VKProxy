using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VKProxy.ACME;
using VKProxy.ACME.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class AcmeServiceCollectionExtensions
{
    public static IServiceCollection AddAcmeChallengeCore(this IServiceCollection services, Action<AcmeChallengeOptions> defaultConfig = null, Action<AcmeOptions> config = null)
    {
        var op = new AcmeChallengeOptions();
        defaultConfig?.Invoke(op);
        services.AddSingleton(op);
        services.AddACME(config);
        services.AddSingleton<ICertificateSource, DeveloperCertSource>();
        services.AddSingleton<ICertificateSource, X509CertStoreSource>();
        services.AddSingleton<ServerCertificateSelector>();
        services.AddSingleton<IServerCertificateSelector>(i => i.GetRequiredService<ServerCertificateSelector>());
        services.TryAddSingleton<IServerCertificateSource>(i => i.GetRequiredService<ServerCertificateSelector>());
        services.AddSingleton<IAcmeStateIniter, InitAcmeStateIniter>();
        services.AddTransient<BeginCertificateCreationAcmeState>();
        services.AddTransient<CheckForRenewalAcmeState>();
        services.AddSingleton<Http01DomainValidator>();
        services.AddSingleton<Dns01DomainValidator>();
        services.AddSingleton<TlsAlpn01DomainValidator>();
        services.TryAddSingleton<IHttpChallengeResponseStore, InMemoryHttpChallengeResponseStore>();
        services.TryAddSingleton<IDnsChallengeStore, NothingDnsChallengeStore>();
        services.TryAddSingleton<ITlsAlpnChallengeStore, TlsAlpnChallengeStore>();
        services.AddSingleton<IStartupFilter, HttpChallengeStartupFilter>()
            .AddSingleton<HttpChallengeResponseMiddleware>();
        services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelOptionsSetup>();
        return services;
    }

    public static IServiceCollection AddAcmeChallenge(this IServiceCollection services, Action<AcmeChallengeOptions> action, Action<AcmeOptions> config = null)
    {
        services.AddAcmeChallengeCore(op =>
        {
            action?.Invoke(op);
            op.Check();
        }, config);
        services.AddSingleton<IHostedService, AcmeLoader>();
        return services;
    }

    public static HttpsConnectionAdapterOptions UseAcmeChallenge(
       this HttpsConnectionAdapterOptions httpsOptions,
       IServerCertificateSelector certificateSelector)
    {
        var otherHandler = httpsOptions.OnAuthenticate;
        httpsOptions.OnAuthenticate = (ctx, options) =>
        {
            certificateSelector.OnSslAuthenticate(ctx, options);
            otherHandler?.Invoke(ctx, options);
        };
        var fallbackSelector = httpsOptions.ServerCertificateSelector;
        httpsOptions.ServerCertificateSelector = (connectionContext, domainName) =>
        {
            var primaryCert = certificateSelector.Select(connectionContext!, domainName);
            return primaryCert ?? fallbackSelector?.Invoke(connectionContext, domainName);
        };

        return httpsOptions;
    }

    public static IApplicationBuilder UseHttpChallengeResponseMiddleware(this IApplicationBuilder app)
    {
        app.Map("/.well-known/acme-challenge", mapped =>
        {
            mapped.UseMiddleware<HttpChallengeResponseMiddleware>();
        });
        return app;
    }
}
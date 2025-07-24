using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace VKProxy.ACME.AspNetCore;

internal class KestrelOptionsSetup : IConfigureOptions<KestrelServerOptions>
{
    private readonly IServerCertificateSelector selector;

    public KestrelOptionsSetup(IServerCertificateSelector certificateSelector)
    {
        selector = certificateSelector ?? throw new ArgumentNullException(nameof(certificateSelector));
    }

    public void Configure(KestrelServerOptions options)
    {
        options.ConfigureHttpsDefaults(o => o.UseAcmeChallenge(selector));
    }
}
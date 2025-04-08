using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace VKProxy.Core.Adapters;

internal sealed class KestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
{
    private readonly IServiceProvider _services;

    public KestrelServerOptionsSetup(IServiceProvider services)
    {
        _services = services;
    }

    public void Configure(KestrelServerOptions options)
    {
        options.ApplicationServices = _services;
    }
}
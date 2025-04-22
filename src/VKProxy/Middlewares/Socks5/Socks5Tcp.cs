using Microsoft.AspNetCore.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKProxy.Features;

namespace VKProxy.Middlewares.Socks5;

internal class Socks5TcpMiddleware : ITcpProxyMiddleware
{
    public Task InitAsync(ConnectionContext context, CancellationToken token, TcpDelegate next)
    {
        var feature = context.Features.Get<IL4ReverseProxyFeature>();
        if (feature is not null)
        {
            var route = feature.Route;
            if (route is not null && route.Metadata is not null
                && route.Metadata.TryGetValue("socks5", out var b) && bool.TryParse(b, out var isSocks5))
            {
                return Proxy(context, feature, token);
            }
        }
        return next(context, token);
    }

    public Task<ReadOnlyMemory<byte>> OnRequestAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        return next(context, source, token);
    }

    public Task<ReadOnlyMemory<byte>> OnResponseAsync(ConnectionContext context, ReadOnlyMemory<byte> source, CancellationToken token, TcpProxyDelegate next)
    {
        return next(context, source, token);
    }

    private async Task Proxy(ConnectionContext context, IL4ReverseProxyFeature feature, CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
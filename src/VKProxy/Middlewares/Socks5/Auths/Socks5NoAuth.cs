using Microsoft.AspNetCore.Connections;
using VKProxy.Features;

namespace VKProxy.Middlewares.Socks5;

public class Socks5NoAuth : ISocks5Auth
{
    private static readonly ReadOnlyMemory<byte> Respone = new byte[] { 0x05, 0x00 }.AsMemory();

    public byte AuthType => 0x00;

    public async ValueTask<bool> AuthAsync(ConnectionContext context, CancellationToken token)
    {
        var r = await context.Transport.Output.WriteAsync(Respone, token).ConfigureAwait(false);
        if (r.IsCanceled || r.IsCompleted)
            return false;
        return true;
    }

    public bool CanAuth(ConnectionContext context)
    {
        var feature = context.Features.Get<IL4ReverseProxyFeature>();
        var route = feature.Route;
        if (route.Metadata.TryGetValue("DisableNoAuth", out var b) && bool.TryParse(b, out var bb) && bb)
        {
            return false;
        }
        return true;
    }
}
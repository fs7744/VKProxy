using Microsoft.AspNetCore.Connections;

namespace VKProxy.Middlewares.Socks5;

public class Socks5NoAuth : ISocks5Auth
{
    private static readonly ReadOnlyMemory<byte> Respone = new byte[] { 0x05, 0x00 }.AsMemory();

    public int Order => 0;

    public byte AuthType => 0x00;

    public async ValueTask<bool> AuthAsync(ConnectionContext context, CancellationToken token)
    {
        var r = await context.Transport.Output.WriteAsync(Respone, token).ConfigureAwait(false);
        if (r.IsCanceled || r.IsCompleted)
            return false;
        return true;
    }
}

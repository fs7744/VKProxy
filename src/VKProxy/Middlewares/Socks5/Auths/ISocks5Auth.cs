using Microsoft.AspNetCore.Connections;

namespace VKProxy.Middlewares.Socks5;

public interface ISocks5Auth
{
    public byte AuthType { get; }

    public ValueTask<bool> AuthAsync(ConnectionContext context, CancellationToken token);

    bool CanAuth(ConnectionContext context);
}
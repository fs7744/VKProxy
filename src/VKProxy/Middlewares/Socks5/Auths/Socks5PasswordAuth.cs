using Microsoft.AspNetCore.Connections;
using System.Buffers;
using VKProxy.Core.Buffers;
using VKProxy.Features;
using VKProxy.Core;

namespace VKProxy.Middlewares.Socks5;

public class Socks5PasswordAuth : ISocks5Auth
{
    private static readonly ReadOnlyMemory<byte> Respone = new byte[] { 0x05, 0x02 }.AsMemory();
    private static readonly ReadOnlyMemory<byte> Success = new byte[] { 0x01, 0x00 }.AsMemory();
    private static readonly ReadOnlyMemory<byte> Failed = new byte[] { 0x01, 0x01 }.AsMemory();

    public byte AuthType => 0x02;

    public async ValueTask<bool> AuthAsync(ConnectionContext context, CancellationToken token)
    {
        var r = await DoAuth(context, token).ConfigureAwait(false);
        if (!r)
        {
            await context.Transport.Output.WriteAsync(Failed, token).ConfigureAwait(false);
        }

        return r;
    }

    private static async ValueTask<bool> DoAuth(ConnectionContext context, CancellationToken token)
    {
        var feature = context.Features.Get<Socks5PasswordAuthFeature>();
        if (feature is not null)
        {
            var s = await context.Transport.Output.WriteAsync(Respone, token).ConfigureAwait(false);
            if (s.IsCanceled || s.IsCompleted)
                return false;
            var input = context.Transport.Input;
            var r = await input.ReadAtLeastAsync(2, token).ConfigureAwait(false);
            var b = r.Buffer.Slice(0, 2);
            var ulen = b.ToSpan()[1];
            input.AdvanceTo(b.End);
            r = await input.ReadAtLeastAsync(ulen + 1, token).ConfigureAwait(false);
            b = r.Buffer.Slice(0, ulen + 1);
            string user;
            byte plen;
            {
                var reader = new SequenceReader<byte>(b);
                user = SequenceReader.ReadString(ref reader, ulen);
                if (!reader.TryRead(out plen))
                {
                    input.AdvanceTo(b.End);
                    return false;
                }
                input.AdvanceTo(b.End);
                if (!string.Equals(feature.User, user, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            r = await input.ReadAtLeastAsync(plen, token).ConfigureAwait(false);
            b = r.Buffer.Slice(0, plen);
            {
                var reader = new SequenceReader<byte>(b);
                var password = SequenceReader.ReadString(ref reader, plen);
                input.AdvanceTo(b.End);
                if (!string.Equals(feature.Password, password, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            var rr = await context.Transport.Output.WriteAsync(Success, token).ConfigureAwait(false);
            if (rr.IsCanceled || r.IsCompleted)
                return false;
            return true;
        }
        return false;
    }

    public bool CanAuth(ConnectionContext context)
    {
        var feature = context.Features.Get<IL4ReverseProxyFeature>();
        var route = feature.Route;
        if (route.Metadata.TryGetValue("socks5User", out var user) && route.Metadata.TryGetValue("socks5Password", out var password))
        {
            context.Features.Set<Socks5PasswordAuthFeature>(new Socks5PasswordAuthFeature()
            {
                User = user,
                Password = password,
            });
            return true;
        }
        return false;
    }
}
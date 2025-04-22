using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using VKProxy.Core.Buffers;

namespace VKProxy.Middlewares.Socks5;

public class Socks5Parser
{
    /// <summary>
    ///|VER | NMETHODS | METHODS  |
    ///+----+----------+----------+
    ///| 5  |    1     | 1 to 255 |
    ///METHODS:
    ///0x00：不需要认证
    ///0x01：GSSAPI
    ///0x02：用户名和密码
    ///0x03 - 0x7F：预留给IANA定义新的标准认证方法
    ///0x80 - 0xFE：预留给私有自定义的认证方法
    /// </summary>
    public static async ValueTask<bool> AuthAsync(PipeReader input, IDictionary<byte, ISocks5Auth> auths, ConnectionContext context, CancellationToken token)
    {
        var r = await input.ReadAtLeastAsync(3, token).ConfigureAwait(false);
        var end = r.Buffer.FirstSpan[1];
        var len = end + 2;
        if (r.Buffer.Length < len)
        {
            input.AdvanceTo(r.Buffer.Start, r.Buffer.End);
            r = await input.ReadAtLeastAsync(len, token).ConfigureAwait(false);
        }
        var rr = r.Buffer.Slice(2, end);
        ISocks5Auth auth = FindAuth(auths, rr);
        input.AdvanceTo(rr.End);
        if (auth == null) return false;
        return await auth.AuthAsync(context, token);

        static ISocks5Auth FindAuth(IDictionary<byte, ISocks5Auth> auths, ReadOnlySequence<byte> rr)
        {
            ISocks5Auth auth = null;
            foreach (var c in rr)
            {
                foreach (var b in c.Span)
                {
                    if (auths.TryGetValue(b, out var a))
                    {
                        if (auth == null || auth.Order < a.Order)
                        {
                            auth = a;
                        }
                    }
                }
            }

            return auth;
        }
    }

    /// <summary>
    ///|VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
    ///+----+-----+-------+------+----------+----------+
    ///| 5  |  1  | X'00' |  1   | Variable |    2     |
    ///CMD：1字节，命令编号，取值为：
    /// 0x01：CONNECT，用于TCP流量代理
    /// 0x02：BIND，可用于代理客户端的某个监听端口来接受外部的主动连接。
    /// 0x03：UDP ASSOCIATE，用于UDP流量代理对于TCP流量代理，该字段取值必须为0x01。
    ///RSV：1字节，保留字段，必须为0x00。
    ///ATYP、DST.ADDR、DST.PORT：在TCP流量代理中，表示要连接到的目标服务器地址。
    ///ATYP：1字节，表示地址类型：
    ///0x01：IPv4，此时XXX.ADDR为4字节的IPv4地址。
    ///0x04：IPv6，此时XXX.ADDR为16字节的IPv6地址。
    ///0x03：域名，此时XXX.ADDR中第一个字节表示域名字符串的长度，后面接域名字符串（不包括最后的\0终止符）。例如域名www.example.com的表示方法为：第一个字节值为15（0x0F），后面的15个字节内容为www.example.com。
    ///XXX.PORT：2字节，端口号。
    /// </summary>
    public static async ValueTask<Socks5CmdRequest> GetCmdRequestAsync(PipeReader input, CancellationToken token)
    {
        var r = await input.ReadAtLeastAsync(8, token).ConfigureAwait(false);
        var addressType = (Socks5Address)r.Buffer.FirstSpan[3];
        var len = addressType switch
        {
            Socks5Address.Ipv4 => 10,
            Socks5Address.Ipv6 => 22,
            Socks5Address.Domain => r.Buffer.FirstSpan[4] + 7
        };
        if (r.Buffer.Length < len)
        {
            input.AdvanceTo(r.Buffer.Start, r.Buffer.End);
            r = await input.ReadAtLeastAsync(len, token).ConfigureAwait(false);
        }
        var s = r.Buffer.Slice(0, len);
        Socks5CmdRequest req = ParseCmdRequest(addressType, s);
        input.AdvanceTo(s.End);
        return req;

        static Socks5CmdRequest ParseCmdRequest(Socks5Address addressType, ReadOnlySequence<byte> s)
        {
            var d = s.ToSpan();
            var req = new Socks5CmdRequest()
            {
                Cmd = (Socks5Cmd)d[1],
                AddressType = addressType,
            };
            switch (addressType)
            {
                case Socks5Address.Ipv4:
                    req.Ip = new IPAddress(d.Slice(4, 4));
                    req.Port = BinaryPrimitives.ReadUInt16BigEndian(d.Slice(8, 2));
                    break;

                case Socks5Address.Domain:
                    var len = d[4];
                    req.Domain = Encoding.UTF8.GetString(d.Slice(5, len));
                    req.Port = BinaryPrimitives.ReadUInt16BigEndian(d.Slice(len + 5, 2));
                    break;

                case Socks5Address.Ipv6:
                    req.Ip = new IPAddress(d.Slice(4, 16));
                    req.Port = BinaryPrimitives.ReadUInt16BigEndian(d.Slice(20, 2));
                    break;
            }
            return req;
        }
    }

    internal static async ValueTask<FlushResult> ResponeAsync(PipeWriter output, Socks5CmdResponseType type, CancellationToken token)
    {
        return await output.WriteAsync(new byte[] { 5, (byte)type, 0, (byte)Socks5Address.Domain, 0, 0, 0 }, token).ConfigureAwait(false);
    }
}
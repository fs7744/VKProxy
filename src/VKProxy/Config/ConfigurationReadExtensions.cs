using Microsoft.Extensions.Configuration;

namespace VKProxy.Config;

internal static class ConfigurationReadExtensions
{
    public static GatewayProtocols? ReadGatewayProtocols(this IConfiguration configuration, string name)
    {
        if (configuration[name] is string value)
        {
            return Enum.Parse<GatewayProtocols>(value, ignoreCase: true);
        }
        else
        {
            var s = configuration.GetSection(name);
            if (!s.Exists() || (s.GetChildren() is var children && !children.Any())) return null;
            return s.GetChildren().Select(i => i.Value).Where(i => i != null).Select(i => Enum.Parse<GatewayProtocols>(i, ignoreCase: true))
                .Aggregate((i, j) => i | j);
        }
    }

    public static IEnumerable<GatewayProtocols> ToAll(this GatewayProtocols protocols)
    {
        var r = protocols;
        if (protocols.HasFlag(GatewayProtocols.TCP))
        {
            r &= ~GatewayProtocols.TCP;
            yield return GatewayProtocols.TCP;
        }
        if (protocols.HasFlag(GatewayProtocols.UDP))
        {
            r &= ~GatewayProtocols.TCP;
            yield return GatewayProtocols.UDP;
        }
        if (r.HasFlag(GatewayProtocols.HTTP1) || r.HasFlag(GatewayProtocols.HTTP2) || r.HasFlag(GatewayProtocols.HTTP3))
            yield return r;
    }
}
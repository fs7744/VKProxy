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
}
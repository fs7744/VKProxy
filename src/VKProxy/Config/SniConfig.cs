using System.Security.Cryptography.X509Certificates;
using VKProxy.Core.Config;

namespace VKProxy.Config;

public class SniConfig
{
    public string Key { get; set; }
    public int Order { get; set; }
    public string[]? Host { get; set; }
    public SslConfig Tls { get; set; }
    internal X509Certificate2? Certificate { get; set; }
}
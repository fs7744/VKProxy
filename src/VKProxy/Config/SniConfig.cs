using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using VKProxy.Core.Config;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Config;

public class SniConfig
{
    public static readonly TimeSpan DefaultHandshakeTimeout = TimeSpan.FromSeconds(10);

    public string Key { get; set; }
    public int Order { get; set; }
    public string[]? Host { get; set; }
    public CertificateConfig? Certificate { get; set; }
    internal X509Certificate2? X509Certificate2 { get; set; }
    public bool Passthrough { get; set; }

    public TimeSpan HandshakeTimeout { get; set; } = DefaultHandshakeTimeout;

    public SslProtocols? Protocols { get; set; }
    public bool? CheckCertificateRevocation { get; set; }
    public ClientCertificateMode? ClientCertificateMode { get; set; }

    public string? RouteId { get; set; }

    internal RouteConfig? RouteConfig { get; set; }
    internal X509Certificate2Collection? X509CertificateFullChain { get; set; }

    internal HttpsConnectionAdapterOptions? GenerateHttps()
    {
        if (Passthrough) return null;
        var s = new HttpsConnectionAdapterOptions();
        s.HandshakeTimeout = HandshakeTimeout;
        if (Protocols.HasValue)
        {
            s.SslProtocols = Protocols.Value;
        }
        if (CheckCertificateRevocation.HasValue)
        {
            s.CheckCertificateRevocation = CheckCertificateRevocation.Value;
        }
        if (ClientCertificateMode.HasValue)
        {
            s.ClientCertificateMode = ClientCertificateMode.Value;
        }
        if (X509Certificate2 != null)
        {
            s.ServerCertificate = X509Certificate2;
            s.ServerCertificateChain = X509CertificateFullChain;
        }
        return s;
    }

    internal SslServerAuthenticationOptions options;

    internal SslServerAuthenticationOptions GenerateOptions()
    {
        if (options == null && !Passthrough)
        {
            options = new SslServerAuthenticationOptions();
            if (X509Certificate2 != null)
            {
                options.ServerCertificate = X509Certificate2;
                options.ServerCertificateContext = SslStreamCertificateContext.Create(X509Certificate2, additionalCertificates: X509CertificateFullChain);
            }
            if (Protocols.HasValue)
            {
                options.EnabledSslProtocols = Protocols.Value;
            }
            if (CheckCertificateRevocation.HasValue)
            {
                options.CertificateRevocationCheckMode = CheckCertificateRevocation.Value ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            }
            if (ClientCertificateMode.HasValue)
            {
                options.ClientCertificateRequired = ClientCertificateMode.Value == Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.AllowCertificate
                || ClientCertificateMode.Value == Microsoft.AspNetCore.Server.Kestrel.Https.ClientCertificateMode.RequireCertificate;
            }
        }
        return options;
    }

    public static bool Equals(SniConfig? t, SniConfig? other)
    {
        if (other is null)
        {
            return t is null;
        }

        if (t is null)
        {
            return other is null;
        }

        return t.Order == other.Order
            && string.Equals(t.Key, other.Key, StringComparison.OrdinalIgnoreCase)
            && CollectionUtilities.EqualsString(t.Host, other.Host)
            && CertificateConfig.Equals(t.Certificate, other.Certificate)
            && t.Passthrough == other.Passthrough
            && t.HandshakeTimeout == other.HandshakeTimeout
            && t.Protocols == other.Protocols
            && t.CheckCertificateRevocation == other.CheckCertificateRevocation
            && t.ClientCertificateMode == other.ClientCertificateMode
            && string.Equals(t.RouteId, other.RouteId, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is SniConfig o && Equals(this, o);
    }
}
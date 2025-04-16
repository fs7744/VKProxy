using VKProxy.Core.Config;

namespace VKProxy.Config.Validators;

public class SniConfigValidator : IValidator<SniConfig>
{
    private readonly ICertificateLoader certificateLoader;

    public SniConfigValidator(ICertificateLoader certificateLoader)
    {
        this.certificateLoader = certificateLoader;
    }

    public ValueTask<bool> ValidateAsync(SniConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        var r = true;

        if (value != null)
        {
            if (value.Host == null || value.Host.Length == 0 || value.Host.Any(i => string.IsNullOrWhiteSpace(i)))
            {
                exceptions.Add(new ArgumentException($"Sni ({value.Key}) Host can not be empty."));
                r = false;
            }
            else if (value.Certificate == null && !value.Passthrough)
            {
                exceptions.Add(new ArgumentException($"Sni ({value.Key}) Tls can not be empty."));
                r = false;
            }
            else
            {
                try
                {
                    var (c, f) = certificateLoader.LoadCertificate(value.Certificate);
                    value.X509Certificate2 = c;
                    value.X509CertificateFullChain = f;
                    if (c == null)
                        r = false;
                }
                catch (Exception ex)
                {
                    exceptions.Add(new ArgumentException($"Sni ({value.Key}) Tls load failed: {ex.Message}."));
                    r = false;
                }
            }
        }

        return ValueTask.FromResult(r);
    }
}
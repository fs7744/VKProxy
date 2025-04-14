using VKProxy.Core.Config;

namespace VKProxy.Config.Validators;

public class SniConfigValidator : IValidator<SniConfig>
{
    private readonly ICertificateLoader certificateLoader;

    public SniConfigValidator(ICertificateLoader certificateLoader)
    {
        this.certificateLoader = certificateLoader;
    }

    public async Task<bool> ValidateAsync(SniConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        var r = true;

        if (value != null)
        {
            if (value.Host == null || value.Host.Length == 0 || value.Host.Any(i => string.IsNullOrWhiteSpace(i)))
            {
                exceptions.Add(new ArgumentException($"Sni ({value.Key}) Host can not be empty."));
                r = false;
            }
            else if (value.Tls == null)
            {
                exceptions.Add(new ArgumentException($"Sni ({value.Key}) Tls can not be empty."));
                r = false;
            }
            else
            {
                try
                {
                    var (c, f) = certificateLoader.LoadCertificate(value.Tls);
                    value.Certificate = c;
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

        return r;
    }
}
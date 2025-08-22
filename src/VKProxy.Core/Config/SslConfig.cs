using System.Diagnostics.CodeAnalysis;

namespace VKProxy.Core.Config;

public class CertificateConfig
{
    public bool IsPEM => !string.IsNullOrEmpty(PEM);
    public string? PEM { get; set; }
    public string? PEMKey { get; set; }
    public bool IsFileCert => !string.IsNullOrEmpty(Path);

    public string? Path { get; set; }

    public string? KeyPath { get; set; }

    public string? Password { get; set; }

    [MemberNotNullWhen(true, nameof(Subject))]
    public bool IsStoreCert => !string.IsNullOrEmpty(Subject);

    public string? Subject { get; set; }

    public string? Store { get; set; }

    public string? Location { get; set; }

    public bool? AllowInvalid { get; set; }

    public static bool Equals(CertificateConfig? t, CertificateConfig? other)
    {
        if (other is null)
        {
            return t is null;
        }

        if (t is null)
        {
            return other is null;
        }

        return string.Equals(t.PEM, other.PEM, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.PEMKey, other.PEMKey, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Path, other.Path, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.KeyPath, other.KeyPath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Password, other.Password, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Subject, other.Subject, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Store, other.Store, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Location, other.Location, StringComparison.OrdinalIgnoreCase)
            && t.AllowInvalid == other.AllowInvalid;
    }

    public override bool Equals(object? obj)
    {
        return obj is CertificateConfig o && CertificateConfig.Equals(this, o);
    }
}
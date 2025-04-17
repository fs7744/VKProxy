using System.Diagnostics.CodeAnalysis;

namespace VKProxy.Core.Config;

public class CertificateConfig
{
    public bool IsFileCert => !string.IsNullOrEmpty(Path);

    public string? Path { get; init; }

    public string? KeyPath { get; init; }

    public string? Password { get; init; }

    [MemberNotNullWhen(true, nameof(Subject))]
    public bool IsStoreCert => !string.IsNullOrEmpty(Subject);

    public string? Subject { get; init; }

    public string? Store { get; init; }

    public string? Location { get; init; }

    public bool? AllowInvalid { get; init; }

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

        return string.Equals(t.Path, other.Path, StringComparison.OrdinalIgnoreCase)
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
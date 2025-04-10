using System.Diagnostics.CodeAnalysis;

namespace VKProxy.Core.Config;

public class SslConfig
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
}
using System.Text.Json;
using VKProxy.ACME;
using VKProxy.ACME.AspNetCore;
using VKProxy.Core.Extensions;

namespace VKProxy.CommandLine;

internal class AuthzOrderCommand : ArgsCommand<AuthzOrderCommandOptions>
{
    public AuthzOrderCommand() : base("authz", "Get the details of an authorization.")
    {
        AuthzOrderCommandOptions.AddCommonArgs(this);
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        await conetxt.AccountAsync(Args.AccountKey, cancellationToken: token);
        var order = conetxt.Order(Args.Order);
        var auth = await order.AuthorizationAsync(Args.Domain, cancellationToken: token);
        object output = null;
        switch (Args.Challenge)
        {
            case ChallengeType.Http01:
                {
                    var c = await auth.HttpAsync(token);
                    output = new
                    {
                        c.Location,
                        ChallengeUri = $".well-known/acme-challenge/{c.Token}",
                        ChallengeTxt = $"{c.Token}.{conetxt.Account.AccountKey.Thumbprint()}",
                        Resource = await c.GetResourceAsync(token)
                    };
                }
                break;

            case ChallengeType.TlsAlpn01:
                {
                    var c = await auth.TlsAlpnAsync(token);
                    var cert = TlsAlpn01DomainValidator.PrepareChallengeCert(Args.Domain, c.KeyAuthz);
                    output = new
                    {
                        c.Location,
                        Pem = cert.ExportPem(),
                        Resource = await c.GetResourceAsync(token)
                    };
                }
                break;

            case ChallengeType.Dns01:
                {
                    var c = await auth.DnsAsync(token);
                    output = new
                    {
                        c.Location,
                        Domain = Args.Domain.GetAcmeDnsDomain(),
                        DnsTxt = conetxt.Account.AccountKey.DnsTxt(c.Token),
                        Resource = await c.GetResourceAsync(token)
                    };
                }
                break;

            case ChallengeType.Any:
            default:
                break;
        }
        Console.WriteLine(JsonSerializer.Serialize(output, DefaultAcmeHttpClient.JsonSerializerOptions));
        Console.WriteLine();
    }
}

public class AuthzOrderCommandOptions : OrderCommandOptions
{
    public string Domain { get; set; }

    public ChallengeType Challenge { get; set; } = ChallengeType.Http01;

    public static void AddCommonArgs<T>(ArgsCommand<T> command) where T : AuthzOrderCommandOptions, new()
    {
        command.AddArg(new CommandArg("domain", "d", null, $"The domain for the certificate order (like: test.example.org).", s =>
        {
            command.Args.Domain = s;
        }, check: () =>
        {
            return !string.IsNullOrWhiteSpace(command.Args.Domain);
        }));
        command.AddArg(new CommandArg("challenge-type", "c", null, $"The challenge type, http or dns or tls.", s =>
        {
            command.Args.Challenge = s?.ToLower() switch
            {
                "http" => ChallengeType.Http01,
                "dns" => ChallengeType.Dns01,
                "tls" => ChallengeType.TlsAlpn01,
                _ => throw new ArgumentException("challenge-type", "Only support http or dns or tls")
            }
            ;
        }));
        OrderCommandOptions.AddCommonArgs(command);
    }
}
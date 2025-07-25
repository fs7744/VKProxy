using Org.BouncyCastle.Asn1.Tsp;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using VKProxy.ACME;
using VKProxy.ACME.AspNetCore;

namespace VKProxy.CommandLine;

internal class ValidateOrderCommand : ArgsCommand<AuthzOrderCommandOptions>
{
    public ValidateOrderCommand() : base("validate", "Validate the authorization challenge.")
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
        IChallengeContext c = null;
        switch (Args.Challenge)
        {
            case ChallengeType.Http01:
                c = await auth.HttpAsync(token);
                break;

            case ChallengeType.TlsAlpn01:
                c = await auth.DnsAsync(token);
                break;

            case ChallengeType.Dns01:
                c = await auth.DnsAsync(token);
                break;

            case ChallengeType.Any:
            default:
                break;
        }
        var d = c.ValidateAsync(token);
        var output = new
        {
            location = c.Location,
            resource = d,
        };
        Console.WriteLine(JsonSerializer.Serialize(output, DefaultAcmeHttpClient.JsonSerializerOptions));
        Console.WriteLine();
    }
}
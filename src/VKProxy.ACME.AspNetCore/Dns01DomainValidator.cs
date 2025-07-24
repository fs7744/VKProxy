using Microsoft.Extensions.Logging;

namespace VKProxy.ACME.AspNetCore;

public class Dns01DomainValidator : DomainOwnershipValidator
{
    private readonly IDnsChallengeStore challengeStore;

    public Dns01DomainValidator(IDnsChallengeStore challengeStore)
    {
        this.challengeStore = challengeStore;
    }

    public override async Task ValidateOwnershipAsync(string domainName, AcmeStateContext context, IAuthorizationContext authzContext, CancellationToken cancellationToken)
    {
        context.Logger.LogDebug("Validate Dns01 for {domainName}", domainName);
        cancellationToken.ThrowIfCancellationRequested();
        var dnsChallenge = await authzContext.DnsAsync(cancellationToken) ?? throw new AcmeException(
                "Did not receive challenge information for challenge type Dns01");
        var dnsTxt = context.AcmeContext.Account.AccountKey.DnsTxt(dnsChallenge.Token);
        var acmeDomain = domainName.GetAcmeDnsDomain();
        try
        {
            await challengeStore.AddTxtRecordAsync(acmeDomain, dnsTxt, cancellationToken);
            await dnsChallenge.ValidateAsync(cancellationToken);
            await WaitForChallengeResultAsync(domainName, context, authzContext, cancellationToken);
        }
        finally
        {
            await challengeStore.RemoveTxtRecordAsync(acmeDomain, dnsTxt, cancellationToken);
        }
    }
}
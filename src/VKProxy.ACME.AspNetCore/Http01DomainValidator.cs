using Microsoft.Extensions.Logging;

namespace VKProxy.ACME.AspNetCore;

public class Http01DomainValidator : DomainOwnershipValidator
{
    private readonly IHttpChallengeResponseStore challengeStore;

    public Http01DomainValidator(IHttpChallengeResponseStore challengeStore)
    {
        this.challengeStore = challengeStore;
    }

    public override async Task ValidateOwnershipAsync(string domainName, AcmeStateContext context, IAuthorizationContext authzContext, CancellationToken cancellationToken)
    {
        context.Logger.LogDebug("Validate Http01 for {domainName}", domainName);
        cancellationToken.ThrowIfCancellationRequested();

        var httpChallenge = await authzContext.HttpAsync(cancellationToken) ?? throw new AcmeException(
                "Did not receive challenge information for challenge type Http01");
        try
        {
            await challengeStore.AddChallengeResponseAsync(httpChallenge.Token, httpChallenge.KeyAuthz, cancellationToken);
            await httpChallenge.ValidateAsync(cancellationToken);
            await WaitForChallengeResultAsync(domainName, context, authzContext, cancellationToken);
        }
        finally
        {
            await challengeStore.RemoveChallengeResponseAsync(httpChallenge.Token, cancellationToken);
        }
    }
}
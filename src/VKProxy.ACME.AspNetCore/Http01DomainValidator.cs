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
        cancellationToken.ThrowIfCancellationRequested();

        var httpChallenge = await authzContext.HttpAsync(cancellationToken);

        if (httpChallenge == null)
        {
            throw new AcmeException(
                "Did not receive challenge information for challenge type Http01");
        }

        var keyAuth = httpChallenge.KeyAuthz;
        await challengeStore.AddChallengeResponseAsync(httpChallenge.Token, keyAuth, cancellationToken);

        await httpChallenge.ValidateAsync(cancellationToken);

        await WaitForChallengeResultAsync(domainName, context, authzContext, cancellationToken);
    }
}
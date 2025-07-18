using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IChallengeContext : IResourceContext<Challenge>
{
    string Type { get; }
    string Token { get; }
    string KeyAuthz { get; }

    Task<Challenge> ValidateAsync(CancellationToken cancellationToken = default);
}

public class ChallengeContext : ResourceContext<Challenge>, IChallengeContext
{
    public ChallengeContext(IAcmeContext context, Uri location, string type, string token) : base(context, location)
    {
        Type = type;
        Token = token;
    }

    public string Type { get; }

    public string Token { get; }

    public string KeyAuthz => context.Account.AccountKey.KeyAuthorization(Token);

    public async Task<Challenge> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var r = await context.Client.PostAsync<Challenge>(context.Account.Signer, Location, context.Account.Location, context.ConsumeNonceAsync, new { }, context.RetryCount, cancellationToken);
        return r.Resource;
    }
}
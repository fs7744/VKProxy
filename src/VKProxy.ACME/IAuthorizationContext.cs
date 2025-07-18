using System.Runtime.CompilerServices;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IAuthorizationContext : IResourceContext<Authorization>
{
    IAsyncEnumerable<IChallengeContext> GetChallengesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);

    Task<Authorization> DeactivateAsync(CancellationToken cancellationToken = default);
}

public class AuthorizationContext : ResourceContext<Authorization>, IAuthorizationContext
{
    public AuthorizationContext(IAcmeContext context, Uri location) : base(context, location)
    {
    }

    public async Task<Authorization> DeactivateAsync(CancellationToken cancellationToken = default)
    {
        var payload = new Authorization { Status = AuthorizationStatus.Deactivated };
        var r = await context.Client.PostAsync<Authorization>(context.Account.Signer, Location, context.Account.Location, context.ConsumeNonceAsync, payload, context.RetryCount, cancellationToken);
        return r.Resource;
    }

    public async IAsyncEnumerable<IChallengeContext> GetChallengesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var a = await GetResourceAsync(cancellationToken);
        if (a != null && a.Challenges != null)
        {
            foreach (var item in a.Challenges)
            {
                yield return new ChallengeContext(this.context, item.Url, item.Type, item.Token);
            }
        }
    }
}
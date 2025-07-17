using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IAccountContext : IResourceContext<Account>
{
    IKey AccountKey { get; }

    JwsSigner Signer { get; }

    Task<Account> UpdateAsync(IList<string> contact, CancellationToken cancellationToken = default);

    Task<Account> DeactivateAsync(CancellationToken cancellationToken = default);

    Task<Account> ChangeKeyAsync(IKey key, CancellationToken cancellationToken = default);
}

public class ResourceContext<T> : IResourceContext<T>
{
    protected readonly IAcmeContext context;

    public ResourceContext(IAcmeContext context, Uri location)
    {
        this.context = context;
        Location = location;
    }

    public Uri Location { get; set; }

    public virtual async Task<T> GetResourceAsync(CancellationToken cancellationToken = default)
    {
        return (await context.GetResourceAsync<T>(Location, cancellationToken)).Resource;
    }
}

public class AccountContext : ResourceContext<Account>, IAccountContext
{
    public AccountContext(IAcmeContext context, Uri location, Crypto.IKey accountKey) : base(context, location)
    {
        AccountKey = accountKey;
        Signer = new JwsSigner(accountKey);
    }

    public IKey AccountKey { get; private set; }
    public JwsSigner Signer { get; private set; }

    public async Task<Account> DeactivateAsync(CancellationToken cancellationToken = default)
    {
        var res = await context.Client.PostAsync<Account>(Signer, Location, Location, context.ConsumeNonceAsync, new Account { Status = AccountStatus.Deactivated }, context.RetryCount, cancellationToken);
        return res.Resource;
    }

    public async Task<Account> UpdateAsync(IList<string> contact, CancellationToken cancellationToken = default)
    {
        var res = await context.Client.PostAsync<Account>(Signer, Location, Location, context.ConsumeNonceAsync, new Account { Contact = contact.ToList(), TermsOfServiceAgreed = true }, context.RetryCount, cancellationToken);
        return res.Resource;
    }

    public async Task<Account> ChangeKeyAsync(IKey key, CancellationToken cancellationToken = default)
    {
        var keyChange = new
        {
            account = Location,
            oldKey = AccountKey.JsonWebKey,
        };
        var jws = new JwsSigner(key);
        var endpoint = context.Directory.KeyChange;
        var body = jws.Sign(keyChange, url: endpoint);
        var res = await context.Client.PostAsync<Account>(Signer, endpoint, Location, context.ConsumeNonceAsync, body, context.RetryCount, cancellationToken);
        AccountKey = key;
        Signer = jws;
        return res.Resource;
    }
}
using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IAccountContext : IResourceContext<Account>
{
    IKey AccountKey { get; }

    JwsSigner Signer { get; }

    //Task<IOrderListContext> Orders();

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
        return (await context.Client.PostAsync<T>(context.AccountSigner, Location, Location, context.ConsumeNonceAsync, null, context.RetryCount, cancellationToken)).Resource;
    }
}

public class AccountContext : ResourceContext<Account>, IAccountContext
{
    public AccountContext(IAcmeContext context, Uri location, Crypto.IKey accountKey) : base(context, location)
    {
        AccountKey = accountKey;
        Signer = new JwsSigner(accountKey);
    }

    public IKey AccountKey { get; }
    public JwsSigner Signer { get; }

    public Task<Account> DeactivateAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Account> UpdateAsync(IList<string> contact, CancellationToken cancellationToken = default)
    {
        var res = await context.Client.PostAsync<Account>(Signer, Location, Location, context.ConsumeNonceAsync, new Account { Contact = contact.ToList(), TermsOfServiceAgreed = true }, context.RetryCount, cancellationToken);
        return res.Resource;
    }

    public Task<Account> ChangeKeyAsync(IKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
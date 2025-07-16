using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IAccountContext : IResourceContext<Account>
{
    IKey AccountKey { get; }

    //Task<IOrderListContext> Orders();

    Task<Account> UpdateAsync(IList<string> contact = null, bool agreeTermsOfService = false, CancellationToken cancellationToken = default);

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

    public virtual Task<T> GetResourceAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public class AccountContext : ResourceContext<Account>, IAccountContext
{
    public AccountContext(IAcmeContext context, Uri location, Crypto.IKey accountKey) : base(context, location)
    {
        AccountKey = accountKey;
    }

    public IKey AccountKey { get; }

    public Task<Account> DeactivateAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Account> UpdateAsync(IList<string> contact = null, bool agreeTermsOfService = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Account> ChangeKeyAsync(IKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
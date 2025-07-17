using System.Runtime.CompilerServices;
using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IAcmeContext
{
    IAcmeClient Client { get; }

    void TrySetNonce<T>(AcmeResponse<T> response);

    AcmeDirectory Directory { get; }
    JwsSigner AccountSigner { get; }

    int RetryCount { get; set; }

    Task InitAsync(Uri directoryUri, CancellationToken cancellationToken = default);

    Task<IAccountContext> NewAccountAsync(IList<string> contact, bool termsOfServiceAgreed, IKey accountKey,
        string eabKeyId = null, string eabKey = null, string eabKeyAlg = null, CancellationToken cancellationToken = default);

    Task<IAccountContext> AccountAsync(IKey accountKey, CancellationToken cancellationToken = default);

    Task<string> ConsumeNonceAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<Uri> ListOrdersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);

    Task<AcmeResponse<T>> GetResourceAsync<T>(Uri resourceUri, CancellationToken cancellationToken = default);

    Task<Order> GetOrderDetailAsync(Uri orderLocation, CancellationToken cancellationToken = default);
}

public class AcmeContext : IAcmeContext
{
    private readonly IAcmeClient client;

    private AcmeDirectory directory;
    private string nonce;
    private IAccountContext account;

    public AcmeContext(IAcmeClient client)
    {
        this.client = client;
    }

    public AcmeDirectory Directory
    {
        get
        {
            if (this.directory == null)
            {
                throw new ArgumentNullException(nameof(Directory), "Please use InitAsync to init directory server first");
            }
            return this.directory;
        }
    }

    public IAcmeClient Client => client;

    public IAccountContext Account
    {
        get
        {
            if (this.account == null)
            {
                throw new ArgumentNullException(nameof(Account), "Please use NewAccountAsync or AccountAsync first");
            }
            return this.account;
        }
    }

    public JwsSigner AccountSigner => Account.Signer;

    public int RetryCount { get; set; }

    public async Task InitAsync(Uri directoryUri, CancellationToken cancellationToken = default)
    {
        if (directory == null)
        {
            directory = await client.DirectoryAsync(directoryUri, cancellationToken);
        }
    }

    public async Task<string> ConsumeNonceAsync(CancellationToken cancellationToken = default)
    {
        var nonce = Interlocked.Exchange(ref this.nonce, null);
        if (nonce == null)
        {
            this.nonce = (await client.NewNonceAsync(Directory, cancellationToken)).ReplayNonce;
            nonce = Interlocked.Exchange(ref this.nonce, null);
        }

        return nonce;
    }

    public async Task<IAccountContext> NewAccountAsync(IList<string> contact, bool termsOfServiceAgreed, IKey accountKey,
        string eabKeyId = null, string eabKey = null, string eabKeyAlg = null, CancellationToken cancellationToken = default)
    {
        var r = await client.NewAccountAsync(Directory, new Account
        {
            Contact = contact.ToList(),
            TermsOfServiceAgreed = termsOfServiceAgreed
        }, accountKey, ConsumeNonceAsync, eabKeyId, eabKey, eabKeyAlg, RetryCount, cancellationToken);
        account = new AccountContext(this, r.Location, accountKey);
        return account;
    }

    public async Task<IAccountContext> AccountAsync(IKey accountKey, CancellationToken cancellationToken = default)
    {
        var r = await client.NewAccountAsync(Directory, new Account.Payload
        {
            OnlyReturnExisting = true,
        }, accountKey, ConsumeNonceAsync, null, null, null, RetryCount, cancellationToken);
        account = new AccountContext(this, r.Location, accountKey);
        return account;
    }

    public async IAsyncEnumerable<Uri> ListOrdersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var account = await Account.GetResourceAsync(cancellationToken);
        var next = account.Orders;
        while (next != null)
        {
            var resp = await client.GetAsync<OrderList>(next, cancellationToken);
            next = resp.Links["next"].FirstOrDefault();
            if (resp.Resource != null && resp.Resource.Orders != null)
            {
                foreach (var item in resp.Resource.Orders)
                {
                    yield return item;
                }
            }
        }
    }

    public async Task<Order> GetOrderDetailAsync(Uri orderLocation, CancellationToken cancellationToken = default)
    {
        return (await GetResourceAsync<Order>(orderLocation, cancellationToken)).Resource;
    }

    public void TrySetNonce<T>(AcmeResponse<T> response)
    {
        if (response != null && response.ReplayNonce != null)
        {
            nonce = response.ReplayNonce;
        }
    }

    public Task<AcmeResponse<T>> GetResourceAsync<T>(Uri resourceUri, CancellationToken cancellationToken)
    {
        return Client.PostAsync<T>(AccountSigner, resourceUri, account.Location, ConsumeNonceAsync, null, RetryCount, cancellationToken);
    }
}
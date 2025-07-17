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
            Contact = contact,
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

    public void TrySetNonce<T>(AcmeResponse<T> response)
    {
        if (response != null && response.ReplayNonce != null)
        {
            nonce = response.ReplayNonce;
        }
    }
}
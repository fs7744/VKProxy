using System.Runtime.CompilerServices;
using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IAcmeContext
{
    IAcmeClient Client { get; }

    AcmeDirectory Directory { get; }

    IAccountContext Account { get; }

    int RetryCount { get; set; }

    Task InitAsync(Uri directoryUri, CancellationToken cancellationToken = default);

    Task<IAccountContext> NewAccountAsync(IList<string> contact, bool termsOfServiceAgreed, Key accountKey,
        string eabKeyId = null, string eabKey = null, string eabKeyAlg = null, CancellationToken cancellationToken = default);

    Task<IAccountContext> AccountAsync(Key accountKey, CancellationToken cancellationToken = default);

    Task<string> ConsumeNonceAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<IOrderContext> ListOrdersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);

    Task<AcmeResponse<T>> GetResourceAsync<T>(Uri resourceUri, CancellationToken cancellationToken = default);

    Task<IOrderContext> NewOrderAsync(IList<string> identifiers, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null, CancellationToken cancellationToken = default);

    Task RevokeCertificateAsync(byte[] certificate, RevocationReason reason, Key certificatePrivateKey, CancellationToken cancellationToken = default);
}

public class AcmeContext : IAcmeContext
{
    private readonly IAcmeClient client;

    private AcmeDirectory directory;
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

    public int RetryCount { get; set; } = 1;

    public async Task InitAsync(Uri directoryUri, CancellationToken cancellationToken = default)
    {
        if (directory == null)
        {
            directory = await client.DirectoryAsync(directoryUri, cancellationToken);
        }
    }

    public async Task<string> ConsumeNonceAsync(CancellationToken cancellationToken = default)
    {
        return (await client.NewNonceAsync(Directory, cancellationToken)).ReplayNonce;
    }

    public async Task<IAccountContext> NewAccountAsync(IList<string> contact, bool termsOfServiceAgreed, Key accountKey,
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

    public async Task<IAccountContext> AccountAsync(Key accountKey, CancellationToken cancellationToken = default)
    {
        var r = await client.NewAccountAsync(Directory, new Account.Payload
        {
            OnlyReturnExisting = true,
        }, accountKey, ConsumeNonceAsync, null, null, null, RetryCount, cancellationToken);
        account = new AccountContext(this, r.Location, accountKey);
        return account;
    }

    public async IAsyncEnumerable<IOrderContext> ListOrdersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var account = await Account.GetResourceAsync(cancellationToken);
        var next = account.Orders;
        while (next != null)
        {
            //var resp = await client.GetAsync<OrderList>(next, cancellationToken);
            var resp = await GetResourceAsync<OrderList>(next, cancellationToken);
            next = resp.Links["next"].FirstOrDefault();
            if (resp.Resource != null && resp.Resource.Orders != null)
            {
                foreach (var item in resp.Resource.Orders)
                {
                    yield return new OrderContext(this, item);
                }
            }
        }
    }

    public Task<AcmeResponse<T>> GetResourceAsync<T>(Uri resourceUri, CancellationToken cancellationToken)
    {
        return Client.PostAsync<T>(Account.Signer, resourceUri, Account.Location, ConsumeNonceAsync, null, RetryCount, cancellationToken);
    }

    public async Task<IOrderContext> NewOrderAsync(IList<string> identifiers, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null, CancellationToken cancellationToken = default)
    {
        var endpoint = Directory.NewOrder;
        var body = new Order
        {
            Identifiers = identifiers
                    .Select(id => new Identifier { Type = IdentifierType.Dns, Value = id })
                    .ToArray(),
            NotBefore = notBefore,
            NotAfter = notAfter,
        };
        var order = await Client.PostAsync<Order>(Account.Signer, endpoint, Account.Location, ConsumeNonceAsync, body, RetryCount, cancellationToken);
        return new OrderContext(this, order.Location);
    }

    public async Task RevokeCertificateAsync(byte[] certificate, RevocationReason reason, Key certificatePrivateKey, CancellationToken cancellationToken = default)
    {
        var endpoint = Directory.RevokeCert;
        var body = new CertificateRevocation
        {
            Certificate = JwsConvert.ToBase64String(certificate),
            Reason = reason
        };
        if (certificatePrivateKey != null)
        {
            var jws = new JwsSigner(certificatePrivateKey);
            await Client.PostAsync<string>(jws, endpoint, null, ConsumeNonceAsync, body, RetryCount, cancellationToken);
        }
        else
        {
            await Client.PostAsync<string>(Account.Signer, endpoint, Account.Location, ConsumeNonceAsync, body, RetryCount, cancellationToken);
        }
    }
}
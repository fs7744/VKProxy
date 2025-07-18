using System.Runtime.CompilerServices;
using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IOrderContext : IResourceContext<Order>
{
    IAsyncEnumerable<IAuthorizationContext> GetAuthorizationsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default);

    Task<Order> FinalizeAsync(byte[] csr, CancellationToken cancellationToken = default);

    Task<CertificateChain> DownloadAsync(string preferredChain = null, CancellationToken cancellationToken = default);
}

internal class OrderContext : ResourceContext<Order>, IOrderContext
{
    public OrderContext(IAcmeContext context, Uri location) : base(context, location)
    {
    }

    public async IAsyncEnumerable<IAuthorizationContext> GetAuthorizationsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var order = await GetResourceAsync(cancellationToken);
        if (order != null && order.Authorizations != null)
        {
            foreach (var item in order.Authorizations)
            {
                yield return new AuthorizationContext(this.context, item);
            }
        }
    }

    public async Task<Order> FinalizeAsync(byte[] csr, CancellationToken cancellationToken = default)
    {
        var order = await GetResourceAsync();
        var payload = new Order.Payload { Csr = JwsConvert.ToBase64String(csr) };
        var resp = await context.Client.PostAsync<Order>(context.Account.Signer, order.Finalize, context.Account.Location, context.ConsumeNonceAsync, payload, context.RetryCount, cancellationToken);
        return resp.Resource;
    }

    public async Task<CertificateChain> DownloadAsync(string preferredChain = null, CancellationToken cancellationToken = default)
    {
        var order = await GetResourceAsync();
        var resp = await context.Client.PostAsync<string>(context.Account.Signer, order.Certificate, context.Account.Location, context.ConsumeNonceAsync, null, context.RetryCount, cancellationToken);

        var defaultChain = new CertificateChain(resp.Resource);
        if (defaultChain.MatchesPreferredChain(preferredChain) || !resp.Links.Contains("alternate"))
            return defaultChain;

        var alternateLinks = resp.Links["alternate"].ToList();
        foreach (var alternate in alternateLinks)
        {
            resp = await context.Client.PostAsync<string>(context.Account.Signer, alternate, context.Account.Location, context.ConsumeNonceAsync, null, context.RetryCount, cancellationToken);
            var chain = new CertificateChain(resp.Resource);

            if (chain.MatchesPreferredChain(preferredChain))
                return chain;
        }

        return defaultChain;
    }
}
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME.AspNetCore;

public class BeginCertificateCreationAcmeState : AcmeState
{
    private readonly ServerCertificateSelector selector;
    private readonly IEnumerable<ICertificateSource> sources;

    public BeginCertificateCreationAcmeState(ServerCertificateSelector selector, IEnumerable<ICertificateSource> sources)
    {
        this.selector = selector;
        this.sources = sources;
    }

    public override async Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken)
    {
        await context.InitAsync(stoppingToken);
        var expectedDomains = new HashSet<string>(context.Options.DomainNames);
        IOrderContext? orderContext = null;
        await foreach (var order in context.AcmeContext.ListOrdersAsync(stoppingToken))
        {
            var orderDetails = await order.GetResourceAsync(stoppingToken);
            if (orderDetails.Status != OrderStatus.Pending)
            {
                continue;
            }
            if (expectedDomains.SetEquals(orderDetails
                    .Identifiers
                    .Where(i => i.Type == IdentifierType.Dns)
                    .Select(s => s.Value)))
            {
                context.Logger.LogDebug("Found an existing order for a certificate");
                orderContext = order;
                break;
            }
        }

        if (orderContext == null)
        {
            context.Logger.LogDebug("Creating new order for a certificate");
            orderContext = await context.AcmeContext.NewOrderAsync(context.Options.DomainNames, cancellationToken: stoppingToken);
        }

        List<Task> tasks = new List<Task>();
        await foreach (var authorization in orderContext.GetAuthorizationsAsync(stoppingToken))
        {
            tasks.Add(ValidateDomainOwnershipAsync(authorization, stoppingToken));
        }
        await Task.WhenAll(tasks);
        var cert = await CompleteCertificateRequestAsync(orderContext, stoppingToken);
        await SaveCertificateAsync(cert, stoppingToken);

        return MoveTo<CheckForRenewalAcmeState>();
    }

    private async Task SaveCertificateAsync(X509Certificate2 cert, CancellationToken stoppingToken)
    {
        selector.Add(cert);

        foreach (var repo in sources)
        {
            try
            {
                await repo.SaveAsync(cert, stoppingToken);
            }
            catch (Exception ex)
            {
                context.Logger.LogError(ex.Message, ex);
            }
        }
    }

    private async Task<X509Certificate2> CompleteCertificateRequestAsync(IOrderContext orderContext, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }

    private async Task ValidateDomainOwnershipAsync(IAuthorizationContext authorizationContext, CancellationToken stoppingToken)
    {
        var authorization = await authorizationContext.GetResourceAsync(stoppingToken);
        var domainName = authorization.Identifier.Value;

        if (authorization.Status == AuthorizationStatus.Valid)
        {
            return;
        }

        GetChallengeValidators();
    }

    private IEnumerable<DomainOwnershipValidator> GetChallengeValidators()
    {
        throw new NotImplementedException();
        if (context.Options.AllowedChallengeTypes.HasFlag(ChallengeType.Http01))
        {
        }

        if (context.Options.AllowedChallengeTypes.HasFlag(ChallengeType.Dns01))
        {
        }

        if (context.Options.AllowedChallengeTypes.HasFlag(ChallengeType.TlsAlpn01))
        {
        }
    }
}

public class DomainOwnershipValidator
{
}
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VKProxy.ACME.Resource;
using VKProxy.Core.Config;

namespace VKProxy.ACME.AspNetCore;

public class BeginCertificateCreationAcmeState : AcmeState
{
    private readonly ServerCertificateSelector selector;
    private readonly IEnumerable<ICertificateSource> sources;
    private readonly Http01DomainValidator http01;
    private readonly Dns01DomainValidator dns01;
    private readonly TlsAlpn01DomainValidator tlsAlpn01;

    public BeginCertificateCreationAcmeState(ServerCertificateSelector selector, IEnumerable<ICertificateSource> sources,
        Http01DomainValidator http01, Dns01DomainValidator dns01, TlsAlpn01DomainValidator tlsAlpn01)
    {
        this.selector = selector;
        this.sources = sources;
        this.http01 = http01;
        this.dns01 = dns01;
        this.tlsAlpn01 = tlsAlpn01;
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
        stoppingToken.ThrowIfCancellationRequested();
        var commonName = context.Options.DomainNames[0];
        context.Logger.LogDebug("Creating cert for {commonName}", commonName);

        var csrInfo = new CsrInfo
        {
            CommonName = commonName,
        };
        Key privateKey = context.Options.KeyAlgorithm.NewKey(context.Options.KeySize);
        var acmeCert = await orderContext.GenerateAsync(csrInfo, privateKey, cancellationToken: stoppingToken);
        var pfxBuilder = acmeCert.ToPfx(privateKey);
        if (context.Options.AdditionalIssuers != null)
        {
            foreach (var item in context.Options.AdditionalIssuers)
            {
                pfxBuilder.AddIssuer(Encoding.UTF8.GetBytes(item));
            }
        }
        var pfx = pfxBuilder.Build("HTTPS Cert - " + context.Options.DomainNames, string.Empty);
        var r = X509CertificateLoader.LoadPkcs12(pfx, string.Empty, X509KeyStorageFlags.Exportable);
        if (OperatingSystem.IsWindows())
        {
            return CertificateLoader.PersistKey(r);
        }
        return r;
    }

    private async Task ValidateDomainOwnershipAsync(IAuthorizationContext authorizationContext, CancellationToken stoppingToken)
    {
        var authorization = await authorizationContext.GetResourceAsync(stoppingToken);
        var domainName = authorization.Identifier.Value;

        if (authorization.Status == AuthorizationStatus.Valid)
        {
            return;
        }

        foreach (var validator in GetChallengeValidators())
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                await validator.ValidateOwnershipAsync(domainName, context, authorizationContext, stoppingToken);
                return;
            }
            catch (Exception ex)
            {
                context.Logger.LogDebug(ex, "Validation with {validatorType} failed with error: {error}",
                    validator.GetType().Name, ex.Message);
            }
        }

        throw new AcmeException($"Failed to validate ownership of domainName '{domainName}'");
    }

    private IEnumerable<DomainOwnershipValidator> GetChallengeValidators()
    {
        if (context.Options.AllowedChallengeTypes.HasFlag(ChallengeType.Http01))
        {
            yield return http01;
        }

        if (context.Options.AllowedChallengeTypes.HasFlag(ChallengeType.TlsAlpn01))
        {
            yield return tlsAlpn01;
        }

        if (context.Options.AllowedChallengeTypes.HasFlag(ChallengeType.Dns01))
        {
            yield return dns01;
        }
    }
}
using Microsoft.Extensions.Logging;

namespace VKProxy.ACME.AspNetCore;

public class InitAcmeState : AcmeState
{
    private readonly ServerCertificateSelector selector;

    public InitAcmeState(AcmeChallengeOptions options, IAcmeContext acmeContext, IServiceProvider serviceProvider, ServerCertificateSelector selector)
    {
        this.selector = selector;
        context = new AcmeStateContext(options, acmeContext, serviceProvider);
    }

    public override async Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken)
    {
        //await context.InitAsync(stoppingToken);
        var domainNames = context.Options.DomainNames;
        var hasCertForAllDomains = domainNames.All(selector.HasCertForDomain);
        if (hasCertForAllDomains)
        {
            context.Logger.LogDebug("Certificate for {domainNames} already found.", domainNames);
            return MoveTo<CheckForRenewalAcmeState>();
        }

        return MoveTo<BeginCertificateCreationAcmeState>();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME.AspNetCore;

public interface IAcmeState
{
    Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken);
}

public class AcmeStateContext
{
    public AcmeStateContext(AcmeChallengeOptions options, IAcmeContext acmeContext, IServiceProvider serviceProvider)
    {
        Options = options;
        AcmeContext = acmeContext;
        ServiceProvider = serviceProvider;
        this.Logger = ServiceProvider.GetRequiredService<ILogger<AcmeState>>();
    }

    public AcmeChallengeOptions Options { get; }
    public IAcmeContext AcmeContext { get; }
    public IServiceProvider ServiceProvider { get; }
    public ILogger<AcmeState> Logger { get; }

    public async Task InitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var account = await Options.AccountFunc(AcmeContext, cancellationToken);
        var a = await account.GetResourceAsync(cancellationToken);
        if (a.Status != AccountStatus.Valid)
        {
            if (Options.CanNewAccount)
            {
                account = await Options.AccountFunc(AcmeContext, cancellationToken);
                a = await account.GetResourceAsync(cancellationToken);
                if (a.Status == AccountStatus.Valid)
                    return;
            }
            throw new AcmeException($"the account is no longer valid. Account status: {a.Status}.");
        }
        Logger.LogInformation($"Using account {account.Location}");
    }
}

public abstract class AcmeState : IAcmeState
{
    protected AcmeStateContext context;

    public abstract Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken);

    protected T MoveTo<T>() where T : IAcmeState
    {
        var r = context.ServiceProvider.GetRequiredService<T>();
        if (r is AcmeState state)
        {
            state.context = this.context;
        }
        return r;
    }
}

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
        await context.InitAsync(stoppingToken);
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

public class BeginCertificateCreationAcmeState : AcmeState
{
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
        throw new NotImplementedException();
    }

    private async Task<X509Certificate2> CompleteCertificateRequestAsync(IOrderContext orderContext, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }

    private async Task ValidateDomainOwnershipAsync(IAuthorizationContext authorization, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}

public class CheckForRenewalAcmeState : AcmeState
{
    private readonly ServerCertificateSelector selector;

    public CheckForRenewalAcmeState(ServerCertificateSelector selector)
    {
        this.selector = selector;
    }

    public override async Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var checkPeriod = context.Options.RenewalCheckPeriod;
            var daysInAdvance = context.Options.RenewDaysInAdvance;
            if (!checkPeriod.HasValue || !daysInAdvance.HasValue)
            {
                context.Logger.LogInformation("Automatic certificate renewal is not configured. Stopping {service}",
                    nameof(AcmeLoader));
                return null;
            }

            var domainNames = context.Options.DomainNames;
            context.Logger.LogDebug($"Checking certificates' renewals for {string.Join(", ", domainNames)}");

            foreach (var domainName in domainNames)
            {
                if (!selector.TryGetCertForDomain(domainName, out var cert)
                || cert == null
                    || cert.NotAfter <= DateTimeOffset.Now.DateTime + daysInAdvance.Value)
                {
                    return MoveTo<BeginCertificateCreationAcmeState>();
                }
            }

            await Task.Delay(checkPeriod.Value, stoppingToken);
        }

        return null;
    }
}
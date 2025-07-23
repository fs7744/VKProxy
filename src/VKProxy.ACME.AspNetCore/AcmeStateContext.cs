using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME.AspNetCore;

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
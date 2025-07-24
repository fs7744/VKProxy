using Microsoft.Extensions.DependencyInjection;

namespace VKProxy.ACME.AspNetCore;

public class InitAcmeStateIniter : IAcmeStateIniter
{
    private readonly AcmeChallengeOptions options;
    private readonly IServiceProvider serviceProvider;

    public InitAcmeStateIniter(AcmeChallengeOptions options, IServiceProvider serviceProvider)
    {
        this.options = options;
        this.serviceProvider = serviceProvider;
    }

    public IAcmeState Init(AcmeChallengeOptions options = null)
    {
        return new InitAcmeState(options ?? this.options, serviceProvider.GetRequiredService<IAcmeContext>(), serviceProvider, serviceProvider.GetRequiredService<IServerCertificateSource>());
    }
}
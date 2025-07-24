using System.Security.Cryptography.X509Certificates;

namespace VKProxy.ACME.AspNetCore;

public static class InitAcmeStateIniterExtensions
{
    public static async Task StartAsync(this IAcmeStateIniter initer, AcmeChallengeOptions options = null, CancellationToken cancellationToken = default)
    {
        var state = initer.Init(options);
        while (state != null && !cancellationToken.IsCancellationRequested)
        {
            state = await state.MoveNextAsync(cancellationToken);
        }
    }

    public static Task<X509Certificate2> CreateCertificateAsync(this IAcmeStateIniter initer, AcmeChallengeOptions options, CancellationToken cancellationToken = default)
    {
        var state = (initer.Init(options) as InitAcmeState).MoveTo<BeginCertificateCreationAcmeState>();
        return state.CreateCertificateAsync(cancellationToken);
    }
}
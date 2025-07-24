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
}

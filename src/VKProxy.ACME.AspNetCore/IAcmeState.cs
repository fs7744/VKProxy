namespace VKProxy.ACME.AspNetCore;

public interface IAcmeState
{
    Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken);
}

public interface IAcmeStateIniter
{
    IAcmeState Init(AcmeChallengeOptions options = null);
}
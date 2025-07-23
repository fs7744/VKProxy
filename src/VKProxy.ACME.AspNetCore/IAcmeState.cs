namespace VKProxy.ACME.AspNetCore;

public interface IAcmeState
{
    Task<IAcmeState> MoveNextAsync(CancellationToken stoppingToken);
}
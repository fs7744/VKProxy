namespace VKProxy.ACME.AspNetCore;

public interface IHttpChallengeResponseStore
{
    Task AddChallengeResponseAsync(string token, string keyAuth, CancellationToken cancellationToken);

    Task<string> GetChallengeResponse(string token, CancellationToken cancellationToken);
}
using System.Collections.Concurrent;

namespace VKProxy.ACME.AspNetCore;

internal class InMemoryHttpChallengeResponseStore : IHttpChallengeResponseStore
{
    private readonly ConcurrentDictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

    public Task AddChallengeResponseAsync(string token, string response, CancellationToken cancellationToken)
    {
        values.AddOrUpdate(token, response, (_, _) => response);
        return Task.CompletedTask;
    }

    public Task<string> GetChallengeResponse(string token, CancellationToken cancellationToken)
    {
        return values.TryGetValue(token, out var value) ? Task.FromResult(value) : Task.FromResult<string>(null);
    }

    public Task RemoveChallengeResponseAsync(string token, CancellationToken cancellationToken)
    {
        values.TryRemove(token, out var value);
        return Task.CompletedTask;
    }
}
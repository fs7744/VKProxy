namespace VKProxy.ACME.AspNetCore;

public class NothingDnsChallengeStore : IDnsChallengeStore
{
    public Task AddTxtRecordAsync(string acmeDomain, string dnsTxt, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RemoveTxtRecordAsync(string acmeDomain, string dnsTxt, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
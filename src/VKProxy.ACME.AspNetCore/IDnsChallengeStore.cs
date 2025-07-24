namespace VKProxy.ACME.AspNetCore;

public interface IDnsChallengeStore
{
    Task AddTxtRecordAsync(string acmeDomain, string dnsTxt, CancellationToken cancellationToken);

    Task RemoveTxtRecordAsync(string acmeDomain, string dnsTxt, CancellationToken cancellationToken);
}

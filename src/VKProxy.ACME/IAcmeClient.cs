using VKProxy.ACME.Resource;
using VKProxy.Config;

namespace VKProxy.ACME;

public interface IAcmeClient
{
    Task<AcmeDirectory?> DirectoryAsync(Uri directoryUri, CancellationToken cancellationToken);
}

public class AcmeClient : IAcmeClient
{
    private readonly IAcmeHttpClient httpClient;

    public AcmeClient(IAcmeHttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<AcmeDirectory?> DirectoryAsync(Uri directoryUri, CancellationToken cancellationToken)
    {
        var (res, data) = await httpClient.GetAsync<AcmeDirectory>(directoryUri, cancellationToken);
        return data;
    }
}
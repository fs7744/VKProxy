using VKProxy.ACME.Resource;
using VKProxy.Config;

namespace VKProxy.ACME;

public interface IAcmeContext
{
    AcmeDirectory Directory { get; }

    Task InitAsync(Uri directoryUri, CancellationToken cancellationToken);
}

public class AcmeContext : IAcmeContext
{
    private readonly IAcmeClient client;

    public AcmeDirectory Directory { get; private set; }

    public AcmeContext(IAcmeClient client)
    {
        this.client = client;
    }

    public async Task InitAsync(Uri directoryUri, CancellationToken cancellationToken)
    {
        if (Directory == null)
        {
            Directory = await client.DirectoryAsync(directoryUri, cancellationToken);
        }
    }
}
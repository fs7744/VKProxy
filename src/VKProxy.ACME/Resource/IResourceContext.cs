namespace VKProxy.ACME.Resource;

public interface IResourceContext<T>
{
    IAcmeContext Context { get; }

    Uri Location { get; }

    int RetryAfter { get; }

    Task<T> GetResourceAsync(CancellationToken cancellationToken = default);
}

public class ResourceContext<T> : IResourceContext<T>
{
    protected readonly IAcmeContext context;

    public IAcmeContext Context => context;

    public int RetryAfter { get; protected set; }

    public ResourceContext(IAcmeContext context, Uri location)
    {
        this.context = context;
        Location = location;
    }

    public Uri Location { get; set; }

    public virtual async Task<T> GetResourceAsync(CancellationToken cancellationToken = default)
    {
        return (await context.GetResourceAsync<T>(Location, cancellationToken)).Resource;
    }
}
namespace VKProxy.ACME.Resource;

public interface IResourceContext<T>
{
    Uri Location { get; }

    Task<T> GetResourceAsync(CancellationToken cancellationToken = default);
}
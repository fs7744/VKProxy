namespace VKProxy.Config;

public interface IConfigStorage
{
    Task<long> DeleteClusterAsync(string key, CancellationToken cancellationToken);

    Task<long> DeleteRouteAsync(string key, CancellationToken cancellationToken);

    Task<long> DeleteListenAsync(string key, CancellationToken cancellationToken);

    Task<long> DeleteSniAsync(string key, CancellationToken cancellationToken);

    Task<bool> ExistsClusterAsync(string key, CancellationToken cancellationToken);

    Task<bool> ExistsListenAsync(string key, CancellationToken cancellationToken);

    Task<bool> ExistsRouteAsync(string key, CancellationToken cancellationToken);

    Task<bool> ExistsSniAsync(string key, CancellationToken cancellationToken);

    Task<IEnumerable<ClusterConfig>> GetClusterAsync(string? prefix, CancellationToken cancellationToken);

    Task<IEnumerable<ListenConfig>> GetListenAsync(string prefix, CancellationToken cancellationToken);

    Task<IEnumerable<RouteConfig>> GetRouteAsync(string? prefix, CancellationToken cancellationToken);

    Task<IEnumerable<SniConfig>> GetSniAsync(string? prefix, CancellationToken cancellationToken);

    Task UpdateClusterAsync(ClusterConfig config, CancellationToken cancellationToken);

    Task UpdateListenAsync(ListenConfig config, CancellationToken cancellationToken);

    Task UpdateRouteAsync(RouteConfig config, CancellationToken cancellationToken);

    Task UpdateSniAsync(SniConfig config, CancellationToken cancellationToken);
}
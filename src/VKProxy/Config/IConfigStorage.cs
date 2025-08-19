namespace VKProxy.Config;

public interface IConfigStorage
{
    Task<long> DeleteClusterAsync(string key);

    Task<long> DeleteRouteAsync(string key);

    Task<long> DeleteListenAsync(string key);

    Task<long> DeleteSniAsync(string key);

    Task<bool> ExistsClusterAsync(string key);

    Task<bool> ExistsListenAsync(string key);

    Task<bool> ExistsRouteAsync(string key);

    Task<bool> ExistsSniAsync(string key);

    Task<IEnumerable<ClusterConfig>> GetClusterAsync(string? prefix);

    Task<IEnumerable<ListenConfig>> GetListenAsync(string prefix);

    Task<IEnumerable<RouteConfig>> GetRouteAsync(string? prefix);

    Task<IEnumerable<SniConfig>> GetSniAsync(string? prefix);

    Task UpdateClusterAsync(ClusterConfig config);

    Task UpdateListenAsync(ListenConfig config);

    Task UpdateRouteAsync(RouteConfig config);

    Task UpdateSniAsync(SniConfig config);
}
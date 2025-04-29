using dotnet_etcd.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using VKProxy.Config;

namespace VKProxy.Storages.Etcd;

public static class EtcdHostBuilderExtensions
{
    public static IServiceCollection UseEtcdConfig(this IServiceCollection services, Action<EtcdClientOptions> config, string prefix = "ReverseProxy/")
    {
        if (config != null)
            services.AddEtcdClient(config);
        services.AddSingleton(new EtcdProxyConfigSourceOptions()
        {
            Prefix = prefix ?? "ReverseProxy/"
        });
        services.AddSingleton<IConfigSource<IProxyConfig>, EtcdProxyConfigSource>();
        return services;
    }
}
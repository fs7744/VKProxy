using dotnet_etcd.DependencyInjection;

namespace VKProxy.Storages.Etcd;

public class EtcdProxyConfigSourceOptions
{
    public string? Prefix { get; set; }

    public string? ConnectionString { get; set; }

    public bool UseInsecureChannel { get; set; }
}
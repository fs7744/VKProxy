namespace VKProxy.Storages.Etcd;

public class EtcdProxyConfigSourceOptions
{
    public string? Prefix { get; set; }

    public string[] Address { get; set; }

    public TimeSpan? Delay { get; set; }
}
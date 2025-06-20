﻿using VKProxy.Storages.Etcd;

namespace VKProxy;

public class VKProxyHostOptions
{
    public string Config { get; set; }

    public bool UseSocks5 { get; set; }
    public EtcdProxyConfigSourceOptions? EtcdOptions { get; set; }
    public Sampler Sampler { get; set; }
}
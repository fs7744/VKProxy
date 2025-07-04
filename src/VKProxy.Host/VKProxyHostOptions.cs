﻿using VKProxy.Core.Infrastructure;
using VKProxy.Storages.Etcd;

namespace VKProxy;

public class VKProxyHostOptions
{
    public string Config { get; set; }

    public bool UseSocks5 { get; set; }
    public EtcdProxyConfigSourceOptions? EtcdOptions { get; set; }
    public Sampler Sampler { get; set; }
    public long? MemoryCacheSizeLimit { get; set; }
    public double MemoryCacheCompactionPercentage { get; set; } = 0.05;
    public string Redis { get; set; }
    public int? RedisPoolSize { get; set; } = 10;
    public string? RedisDataProtection { get; set; }
    public DiskCacheOptions DiskCache { get; private set; } = new DiskCacheOptions();
}
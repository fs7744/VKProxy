using DotNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;
using VKProxy.StackExchangeRedis;
using VKProxy.Storages.Etcd;

namespace VKProxy;

public static class VKProxyHost
{
    public static IHostBuilder CreateBuilder(string[] args, Action<Dictionary<string, Func<VKProxyHostOptions, IEnumerator<string>, string>>> action = null, Action<IHostBuilder> hostAction = null)
    {
        try
        {
            var options = LoadFromEnv();
            var handlers = GetHandlers();
            action?.Invoke(handlers);
            var e = (args as IEnumerable<string>).GetEnumerator();
            while (e.MoveNext())
            {
                var err = string.Empty;
                if (handlers.TryGetValue(e.Current, out var h))
                {
                    err = h(options, e);
                }
                else
                {
                    err = $"Not found args: {e.Current}";
                }
                if (!string.IsNullOrEmpty(err))
                {
                    Console.WriteLine(err);
                    break;
                }
            }
            if (!Check(options)) return null;
            return CreateBuilder(options, hostAction);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static VKProxyHostOptions LoadFromEnv()
    {
        var options = new VKProxyHostOptions();
        options.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
        if (!options.EtcdOptions.Address.IsNullOrEmpty())
        {
            options.EtcdOptions = null;
        }
        options.Config = Environment.GetEnvironmentVariable("VKPROXY_CONFIG");
        options.UseSocks5 = bool.TryParse(Environment.GetEnvironmentVariable("VKPROXY_SOCKS5"), out var useSocks5) && useSocks5;
        if (Enum.TryParse<Sampler>(Environment.GetEnvironmentVariable("VKPROXY_SAMPLER") ?? "None", true, out var sampler))
        {
            options.Sampler = sampler;
        }
        else
        {
            options.Sampler = Sampler.None;
        }
        if (long.TryParse(Environment.GetEnvironmentVariable("VKPROXY_MEMORY_CACHE_MAX"), out var max) && max > 0)
        {
            options.MemoryCacheSizeLimit = max;
        }
        if (double.TryParse(Environment.GetEnvironmentVariable("VKPROXY_MEMORY_CACHE_COMPACTION_PERCENTAGE"), out var p) && (p >= 0 && p <= 1))
        {
            options.MemoryCacheCompactionPercentage = p;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("VKPROXY_REDIS_POOL_SIZE"), out var pool) && pool > 0)
        {
            options.RedisPoolSize = pool;
        }
        options.Redis = Environment.GetEnvironmentVariable("VKPROXY_REDIS");
        var path = Environment.GetEnvironmentVariable("VKPROXY_DISK_CACHE");
        if (!string.IsNullOrWhiteSpace(path))
            options.DiskCache.Path = path;
        if (long.TryParse(Environment.GetEnvironmentVariable("VKPROXY_DISK_CACHE_MAX"), out max))
        {
            options.DiskCache.SizeLimmit = max;
        }
        path = Environment.GetEnvironmentVariable("VKPROXY_REDIS_DATA_PROTECTION");
        if (!string.IsNullOrWhiteSpace(path))
            options.RedisDataProtection = path;
        return options;
    }

    public static bool Check(VKProxyHostOptions options)
    {
        if (options.EtcdOptions != null)
        {
            if (!string.IsNullOrWhiteSpace(options.Config))
            {
                Console.WriteLine($"Can't use etcd and file config both");
                return false;
            }

            if (options.EtcdOptions.Address.IsNullOrEmpty())
            {
                Console.WriteLine($"etcd address can't be empty");
                return false;
            }

            if (string.IsNullOrEmpty(options.EtcdOptions.Prefix))
            {
                Console.WriteLine($"etcd prefix can't be empty");
                return false;
            }
        }
        else if (string.IsNullOrWhiteSpace(options.Config))
        {
            Console.WriteLine($"json config file can't be empty");
            return false;
        }
        return true;
    }

    private static Dictionary<string, Func<VKProxyHostOptions, IEnumerator<string>, string>> GetHandlers()
    {
        var r = new Dictionary<string, Func<VKProxyHostOptions, IEnumerator<string>, string>>(StringComparer.OrdinalIgnoreCase);
        r.Add("--config", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                args.Config = en.Current;
                return string.Empty;
            }
            else
            {
                return "config must set path";
            }
        });
        r.Add("-c", r["--config"]);
        r.Add("--Socks5", (args, en) =>
        {
            args.UseSocks5 = true;
            return string.Empty;
        });
        r.Add("--etcd", (args, en) =>
        {
            if (args.EtcdOptions == null)
            {
                args.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
            }
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                args.EtcdOptions.Address = en.Current.Split(",", StringSplitOptions.RemoveEmptyEntries);
                return string.Empty;
            }
            else
            {
                return "must has etcd address";
            }
        });
        r.Add("--etcd-prefix", (args, en) =>
        {
            if (args.EtcdOptions == null)
            {
                args.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
            }
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                args.EtcdOptions.Prefix = en.Current;
                return string.Empty;
            }
            else
            {
                return "must has etcd address";
            }
        });
        r.Add("--ETCD-DELAY", (args, en) =>
        {
            if (args.EtcdOptions == null)
            {
                args.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
            }
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && TimeSpan.TryParse(en.Current, out var t))
            {
                args.EtcdOptions.Delay = t;
                return string.Empty;
            }
            else
            {
                return "Delay must be TimeSpan";
            }
        });
        r.Add("--Sampler", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && Enum.TryParse<Sampler>(en.Current, out var e))
            {
                args.Sampler = e;
                return string.Empty;
            }
            else
            {
                return "Sampler must be Trace/Random/None";
            }
        });
        r.Add("--memory-cache-max", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && long.TryParse(en.Current, out var e) && e > 0)
            {
                args.MemoryCacheSizeLimit = e;
                return string.Empty;
            }
            else
            {
                return "Memory Cache Size Limit must be long";
            }
        });
        r.Add("--memory-cache-percentage", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && double.TryParse(en.Current, out var e) && (e >= 0 && e <= 1))
            {
                args.MemoryCacheCompactionPercentage = e;
                return string.Empty;
            }
            else
            {
                return "Memory Cache Compaction Percentage must be between 0 and 1 inclusive";
            }
        });
        r.Add("--disk-cache", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                args.DiskCache.Path = en.Current;
                return string.Empty;
            }
            else
            {
                return "Disk Cache must be directory";
            }
        });
        r.Add("--disk-cache-max", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && long.TryParse(en.Current, out var e))
            {
                args.DiskCache.SizeLimmit = e;
                return string.Empty;
            }
            else
            {
                return "Disk Cache Size Limit must be long";
            }
        });
        r.Add("--redis", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                args.Redis = en.Current;
                return string.Empty;
            }
            else
            {
                return "must StackExchangeRedis config format";
            }
        });
        r.Add("--redis-data-protection", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                args.RedisDataProtection = en.Current;
                return string.Empty;
            }
            else
            {
                return "DataProtection sotre in redis key";
            }
        });
        r.Add("--redis-pool-size", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && int.TryParse(en.Current, out var e) && e > 0)
            {
                args.RedisPoolSize = e;
                return string.Empty;
            }
            else
            {
                return "StackExchangeRedis pool size must be int";
            }
        });
        r.Add("--help", (args, en) =>
        {
            Console.WriteLine($"--config (-c)               json file config, like /xx/app.json");
            Console.WriteLine($"--socks5                    use simple socks5 support");
            Console.WriteLine($"--etcd                      etcd address, like http://127.0.0.1:2379");
            Console.WriteLine($"--etcd-prefix               default is /ReverseProxy/");
            Console.WriteLine($"--etcd-delay                delay change config when etcd change, default is 00:00:01");
            Console.WriteLine($"--sampler                   log sampling, support trace/random/none");
            Console.WriteLine($"--memory-cache-max          Memory Cache Size Limit");
            Console.WriteLine($"--memory-cache-percentage   Memory Cache Compaction Percentage");
            Console.WriteLine($"--redis                     StackExchangeRedis config");
            Console.WriteLine($"--redis-pool-size           StackExchangeRedis pool size, default is 10");
            Console.WriteLine($"--redis-data-protection     DataProtection sotre in redis key");
            Console.WriteLine($"--disk-cache                disk cache directory");
            Console.WriteLine($"--disk-cache-max            disk cache Size Limit");
            Console.WriteLine($"--help (-h)                 show all options");
            Console.WriteLine("View more at https://fs7744.github.io/VKProxy.Doc/docs/introduction.html");
            throw new NotSupportedException();
        });
        r.Add("-h", r["--help"]);
        return r;
    }

    public static IHostBuilder CreateBuilder(Action<IHostBuilder, VKProxyHostOptions> action = null)
    {
        var options = LoadFromEnv();
        return CreateBuilder(options, b =>
        {
            action?.Invoke(b, options);
        });
    }

    private static IHostBuilder CreateBuilder(VKProxyHostOptions options, Action<IHostBuilder> action = null)
    {
        var b = Host.CreateDefaultBuilder();
        action?.Invoke(b);
        return b
            .ConfigureLogging((HostBuilderContext c, ILoggingBuilder i) =>
            {
                switch (options.Sampler)
                {
                    case Sampler.Random:
                        i.AddRandomProbabilisticSampler(c.Configuration);
                        break;

                    case Sampler.Trace:
                        i.AddTraceBasedSampler();
                        break;

                    default:
                        break;
                }
            })
            .ConfigureHostConfiguration(i =>
            {
                if (!string.IsNullOrWhiteSpace(options.Config))
                    i.AddJsonFile(options.Config);
            })
            .ConfigureServices(i =>
            {
                i.AddMemoryCache(j =>
                {
                    j.SizeLimit = options.MemoryCacheSizeLimit;
                    j.CompactionPercentage = options.MemoryCacheCompactionPercentage;
                });
                i.AddDiskCache(false, o =>
                {
                    if (!string.IsNullOrWhiteSpace(options.DiskCache.Path))
                    {
                        o.Path = options.DiskCache.Path;
                    }
                    o.SizeLimmit = options.DiskCache.SizeLimmit;
                });
                i.AddSingleton<IResponseCache, DiskResponseCache>();
                if (options.UseSocks5)
                {
                    i.UseSocks5();
                    i.UseWSToSocks5();
                }
                if (options.EtcdOptions != null)
                {
                    i.UseEtcdConfig(options.EtcdOptions);
                }

                if (!string.IsNullOrWhiteSpace(options.Redis))
                {
                    var pool = StackExchangeRedisExtensions.BuildPooledRedis(options.Redis, options.RedisPoolSize.GetValueOrDefault(10));
                    i.AddSingleton(pool);
                    i.AddRedisResponseCache();
                    i.AddRedisConcurrency();
                    if (!string.IsNullOrWhiteSpace(options.RedisDataProtection))
                    {
                        i.PersistKeysToStackExchangeRedis(pool, options.RedisDataProtection);
                    }
                }
            })
            .UseReverseProxy();
    }
}
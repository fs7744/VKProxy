using DotNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VKProxy.CommandLine;
using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;
using VKProxy.StackExchangeRedis;
using VKProxy.Storages.Etcd;

namespace VKProxy;

public static class VKProxyHost
{
    public static Task RunAsync(string[] args)
    {
        var f = DoRunAsync(args);

        return f == null ? Task.CompletedTask : f();
    }

    private static Func<Task> DoRunAsync(string[] args)
    {
        var parser = new CommandParser();
        parser.Add(new ProxyCommand(true));
        return parser.Parse(args);
    }

    public static IHostBuilder CreateBuilder(string[] args, Action<IHostBuilder, VKProxyHostOptions> action = null)
    {
        var cmd = new ProxyCommand(false);
        var parser = new CommandParser();
        parser.Add(cmd);
        parser.Parse(new string[] { cmd.Name }.Union(args).ToArray());
        var options = cmd.Args;
        return CreateBuilder(options, b =>
        {
            action?.Invoke(b, options);
        });
    }

    internal static IHostBuilder CreateBuilder(VKProxyHostOptions options, Action<IHostBuilder> action = null)
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
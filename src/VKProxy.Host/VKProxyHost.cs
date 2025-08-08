using DotNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using VKProxy.CommandLine;
using VKProxy.Middlewares.Http;
using VKProxy.Middlewares.Http.HttpFuncs;
using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;
using VKProxy.StackExchangeRedis;
using VKProxy.Storages.Etcd;

namespace VKProxy;

public static class VKProxyHost
{
    public static async Task RunAsync(string[] args)
    {
        if (args.Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
        {
            var f = DoRunAsync(args.Where(i => !i.Equals("--debug", StringComparison.OrdinalIgnoreCase)).ToArray());
            if (f != null)
                await f();
        }
        else
        {
            try
            {
                var f = DoRunAsync(args);
                if (f != null)
                    await f();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static Func<Task> DoRunAsync(string[] args)
    {
        var parser = new CommandParser();
        parser.Add(new VersionCommand());
        parser.Add(new ProxyCommand(true));
        parser.Add(new ACMECommand());
        return parser.Parse(args);
    }

    public static IHostBuilder CreateBuilder(string[] args, Action<IHostBuilder, VKProxyHostOptions> action = null, Action<OpenTelemetryBuilder> configOpenTelemetry = null)
    {
        var cmd = new ProxyCommand(false);
        var parser = new CommandParser();
        parser.Add(cmd);
        parser.Parse(new string[] { cmd.Name }.Union(args).ToArray());
        var options = cmd.Args;
        return CreateBuilder(options, b =>
        {
            action?.Invoke(b, options);
        }, configOpenTelemetry);
    }

    internal static IHostBuilder CreateBuilder(VKProxyHostOptions options, Action<IHostBuilder> action = null, Action<OpenTelemetryBuilder> configOpenTelemetry = null)
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

                if (options.Telemetry)
                {
                    i.AddSingleton<IHttpFunc, PrometheusFunc>();
                    var tb = i.AddOpenTelemetry()
                    .ConfigureResource(resource => resource.AddContainerDetector())
                    .WithTracing(i =>
                    {
                        if (i is IDeferredTracerProviderBuilder deferredTracerProviderBuilder)
                        {
                            deferredTracerProviderBuilder.Configure((sp, builder) =>
                            {
                                var activitySourceService = sp.GetService<ActivitySource>();
                                if (activitySourceService != null)
                                {
                                    builder.AddSource(activitySourceService.Name);
                                }
                            });
                        }
                    })
                    .WithMetrics(builder =>
                    {
                        builder.AddMeter(options.Meters);
                        builder.AddPrometheusExporter();
                    });
                    configOpenTelemetry?.Invoke(tb);
                    if (!options.DropInstruments.IsNullOrEmpty())
                    {
                        i.ConfigureOpenTelemetryMeterProvider(j =>
                        {
                            foreach (var drop in options.DropInstruments)
                            {
                                j.AddView(drop, MetricStreamConfiguration.Drop);
                            }
                        });
                    }
                }
            })
            .UseReverseProxy();
    }
}
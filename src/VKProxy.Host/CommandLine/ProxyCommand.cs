using DotNext;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using VKProxy.Storages.Etcd;

namespace VKProxy.CommandLine;

public class ProxyCommand : ArgsCommand<VKProxyHostOptions>
{
    private readonly bool isRun;

    public ProxyCommand(bool isRun) : base("proxy", "L4/L7 proxy build on Kestrel")
    {
        AddArg(new CommandArg("config", "c", "VKPROXY_CONFIG", "json file config, like /xx/app.json", s =>
        {
            if (File.Exists(s))
                Args.Config = s;
            else
                throw new CommandParseException("File not found exists!");
        }));
        AddArg(new CommandArg("etcd", null, "ETCD_CONNECTION_STRING", "etcd address, like http://127.0.0.1:2379", s =>
        {
            var address = s.Split(",", StringSplitOptions.RemoveEmptyEntries);
            if (address == null || address.Length == 0)
            {
                throw new CommandParseException("Must has etcd address, like http://127.0.0.1:2379!");
            }
            if (Args.EtcdOptions == null)
            {
                Args.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
            }
            Args.EtcdOptions.Address = address;
        }));
        AddArg(new CommandArg("etcd-prefix", null, "ETCD_PREFIX", "default is /ReverseProxy/", s =>
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new CommandParseException("Can't be empty");
            }
            if (Args.EtcdOptions == null)
            {
                Args.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
            }
            Args.EtcdOptions.Prefix = s;
        }));
        AddArg(new CommandArg("etcd-delay", null, "ETCD_DELAY", "delay change config when etcd change, default is 00:00:01", s =>
        {
            if (Args.EtcdOptions == null)
            {
                Args.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
            }
            Args.EtcdOptions.Delay = TimeSpan.Parse(s);
        }));
        AddArg(new CommandArg("socks5", null, "VKPROXY_SOCKS5", "use simple socks5 support", s => Args.UseSocks5 = bool.Parse(s)));
        AddArg(new CommandArg("sampler", null, "VKPROXY_SAMPLER", "log sampling, support trace/random/none", s => Args.Sampler = Enum.Parse<Sampler>(s, true)));
        AddArg(new CommandArg("memory-cache-max", null, "VKPROXY_MEMORY_CACHE_MAX", "Memory Cache Size Limit", s =>
        {
            var v = long.Parse(s);
            if (v <= 0)
                throw new CommandParseException("Must large than 0");
            Args.MemoryCacheSizeLimit = v;
        }));
        AddArg(new CommandArg("memory-cache-percentage", null, "VKPROXY_MEMORY_CACHE_COMPACTION_PERCENTAGE", "Memory Cache Compaction Percentage", s =>
        {
            var v = double.Parse(s);
            if (v < 0 || v > 1)
                throw new CommandParseException("Memory Cache Compaction Percentage must be between 0 and 1 inclusive");
            Args.MemoryCacheCompactionPercentage = v;
        }));
        AddArg(new CommandArg("disk-cache", null, "VKPROXY_DISK_CACHE", "disk cache directory", s =>
        {
            Args.DiskCache.Path = s;
            if (!Directory.Exists(s))
                Directory.CreateDirectory(s);
        }));
        AddArg(new CommandArg("disk-cache-max", null, "VKPROXY_DISK_CACHE_MAX", "disk cache Size Limit", s =>
        {
            var v = long.Parse(s);
            Args.DiskCache.SizeLimmit = v;
        }));
        AddArg(new CommandArg("redis", null, "VKPROXY_REDIS", "StackExchangeRedis config", s =>
        {
            ConfigurationOptions.Parse(s);
            Args.Redis = s;
        }));
        AddArg(new CommandArg("redis-data-protection", null, "VKPROXY_REDIS_DATA_PROTECTION", "DataProtection sotre in redis key", s =>
        {
            Args.RedisDataProtection = s;
        }));
        AddArg(new CommandArg("redis-pool-size", null, "VKPROXY_REDIS_POOL_SIZE", "StackExchangeRedis pool size, default is 10", s =>
        {
            var v = int.Parse(s);
            if (v <= 0)
                throw new CommandParseException("Must large than 0");
            Args.RedisPoolSize = v;
        }));
        AddArg(new CommandArg("telemetry", null, "VKPROXY_TELEMETRY", "Allow export telemetry data (metrics, logs, and traces) to help you analyze your software’s performance and behavior.", s =>
        {
            Args.Telemetry = bool.Parse(s);
        }));
        AddArg(new CommandArg("meter", null, "VKPROXY_TELEMETRY_METER", "Subscribe meters, default is System.Runtime,Microsoft.AspNetCore.Server.Kestrel,Microsoft.AspNetCore.Server.Kestrel.Udp,Microsoft.AspNetCore.MemoryPool,VKProxy.ReverseProxy", s =>
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            var ss = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ss.IsNullOrEmpty()) return;
            Args.Meters = ss;
        }));
        AddArg(new CommandArg("drop_instrument", null, "VKPROXY_TELEMETRY_DROP_INSTRUMENT", "Drop instruments", s =>
        {
            if (s == null) return;
            Args.DropInstruments = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }));
        AddArg(new CommandArg("exporter", null, "VKPROXY_TELEMETRY_EXPORTER", "How to export telemetry data (metrics, logs, and traces), support prometheus,console,otlp , default is otlp, please set env like `OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:4317/` ", s =>
        {
            if (s == null) return;
            Args.Exporter = s;
        }));
        this.isRun = isRun;
    }

    protected override Func<Task> Do()
    {
        if (isRun)
        {
            if (Args.EtcdOptions != null)
            {
                if (!string.IsNullOrWhiteSpace(Args.Config))
                {
                    throw new CommandParseException($"Can't use etcd and file config both");
                }

                if (Args.EtcdOptions.Address.IsNullOrEmpty())
                {
                    throw new CommandParseException($"etcd address can't be empty");
                }

                if (string.IsNullOrEmpty(Args.EtcdOptions.Prefix))
                {
                    throw new CommandParseException($"etcd prefix can't be empty");
                }
            }
            else if (string.IsNullOrWhiteSpace(Args.Config))
            {
                throw new CommandParseException($"json config file can't be empty");
            }

            var options = Args;
            var app = VKProxyHost.CreateBuilder(options).Build();
            return () => app.RunAsync();
        }
        else
            return null;
    }

    protected override async Task ExecAsync()
    {
        throw new NotImplementedException();
    }
}
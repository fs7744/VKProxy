using DotNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        r.Add("--help", (args, en) =>
        {
            Console.WriteLine($"--config (-c)       json file config, like /xx/app.json");
            Console.WriteLine($"--socks5            use simple socks5 support");
            Console.WriteLine($"--etcd              etcd address, like http://127.0.0.1:2379");
            Console.WriteLine($"--etcd-prefix       default is /ReverseProxy/");
            Console.WriteLine($"--etcd-delay        delay change config when etcd change, default is 00:00:01");
            Console.WriteLine($"--sampler           log sampling, support trace/random/none");
            Console.WriteLine($"--help (-h)         show all options");
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
                if (options.UseSocks5)
                {
                    i.UseSocks5();
                    i.UseWSToSocks5();
                }
                if (options.EtcdOptions != null)
                {
                    i.UseEtcdConfig(options.EtcdOptions);
                }
            })
            .UseReverseProxy();
    }
}
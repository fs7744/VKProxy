using DotNext;
using VKProxy.Storages.Etcd;

namespace VKProxy.Cli;

public class Args
{
    public Args(string[] args)
    {
        LoadFromEnv();
        var handlers = GetHandlers();
        var e = (args as IEnumerable<string>).GetEnumerator();
        while (e.MoveNext())
        {
            var err = string.Empty;
            if (handlers.TryGetValue(e.Current, out var h))
            {
                err = h(this, e);
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
    }

    private void LoadFromEnv()
    {
        this.EtcdOptions = EtcdHostBuilderExtensions.LoadEtcdProxyConfigSourceOptionsFromEnv();
        if (!this.EtcdOptions.Address.IsNullOrEmpty())
        {
            UseEtcd = true;
        }
        Config = Environment.GetEnvironmentVariable("VKPROXY_CONFIG");
        UseSocks5 = bool.TryParse(Environment.GetEnvironmentVariable("VKPROXY_SOCKS5"), out var useSocks5) && useSocks5;
    }

    private Dictionary<string, Func<Args, IEnumerator<string>, string>> GetHandlers()
    {
        var r = new Dictionary<string, Func<Args, IEnumerator<string>, string>>(StringComparer.OrdinalIgnoreCase);
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
            args.UseEtcd = true;
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                EtcdOptions.Address = en.Current.Split(",", StringSplitOptions.RemoveEmptyEntries);
                return string.Empty;
            }
            else
            {
                return "must has etcd address";
            }
        });
        r.Add("--etcd-prefix", (args, en) =>
        {
            args.UseEtcd = true;
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-"))
            {
                EtcdOptions.Prefix = en.Current;
                return string.Empty;
            }
            else
            {
                return "must has etcd address";
            }
        });
        r.Add("--ETCD-DELAY", (args, en) =>
        {
            args.UseEtcd = true;
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && TimeSpan.TryParse(en.Current, out var t))
            {
                EtcdOptions.Delay = t;
                return string.Empty;
            }
            else
            {
                return "Delay must be TimeSpan";
            }
        });
        r.Add("--Sampler", (args, en) =>
        {
            if (en.MoveNext() && !string.IsNullOrWhiteSpace(en.Current) && !en.Current.StartsWith("-") && !en.Current.Equals("Trace", StringComparison.OrdinalIgnoreCase) && !en.Current.Equals("Random", StringComparison.OrdinalIgnoreCase))
            {
                args.Sampler = en.Current;
                return string.Empty;
            }
            else
            {
                return "Sampler must be Trace/Random";
            }
        });
        r.Add("--help", (args, en) =>
        {
            args.Help = true;
            Console.WriteLine($"--config (-c)       json file config, like /xx/app.json");
            Console.WriteLine($"--socks5            use simple socks5 support");
            Console.WriteLine($"--etcd              etcd address, like http://127.0.0.1:2379");
            Console.WriteLine($"--etcd-prefix       default is /ReverseProxy/");
            Console.WriteLine($"--etcd-delay        delay change config when etcd change, default is 00:00:01");
            Console.WriteLine($"--sampler           log sampling, support trace/random");
            Console.WriteLine($"--help (-h)         show all options");
            return "View more at https://fs7744.github.io/VKProxy.Doc/docs/introduction.html";
        });
        r.Add("-h", r["--help"]);
        return r;
    }

    public string Config { get; set; }

    public bool UseSocks5 { get; set; }

    public bool UseEtcd { get; set; }
    public EtcdProxyConfigSourceOptions EtcdOptions { get; set; }
    public bool Help { get; set; }
    public string Sampler { get; set; }

    public bool Check()
    {
        if (Help)
        {
            return false;
        }
        if (UseEtcd)
        {
            if (!string.IsNullOrWhiteSpace(Config))
            {
                Console.WriteLine($"Can't use etcd and file config both");
                return false;
            }

            if (EtcdOptions.Address.IsNullOrEmpty())
            {
                Console.WriteLine($"etcd address can't be empty");
                return false;
            }

            if (string.IsNullOrEmpty(EtcdOptions.Prefix))
            {
                Console.WriteLine($"etcd prefix can't be empty");
                return false;
            }
        }
        else if (string.IsNullOrWhiteSpace(Config))
        {
            Console.WriteLine($"json config file can't be empty");
            return false;
        }
        return true;
    }
}
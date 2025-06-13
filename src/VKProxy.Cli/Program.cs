using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VKProxy.Storages.Etcd;

namespace VKProxy.Cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = Build(args);
        if (app != null)
            await app.RunAsync();
    }

    private static IHost Build(string[] s)
    {
        var args = new Args(s);
        if (!args.Check()) return null;
        return Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(i =>
            {
                if (!string.IsNullOrWhiteSpace(args.Config))
                    i.AddJsonFile(args.Config);
            })
            .ConfigureServices(i =>
            {
                if (args.UseSocks5)
                {
                    i.UseSocks5();
                    i.UseWSToSocks5();
                }
                if (args.UseEtcd)
                {
                    i.UseEtcdConfig(args.EtcdOptions);
                }
            })
            .UseReverseProxy()
            .Build();
    }
}
using Etcd;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VKProxy.Config;
using VKProxy.Core.Config;

namespace VKProxy.Storages.Etcd;

public static class EtcdHostBuilderExtensions
{
    internal static readonly TimeSpan defaultDelay = TimeSpan.FromSeconds(1);

    public static IServiceCollection UseEtcdConfig(this IServiceCollection services, EtcdProxyConfigSourceOptions options, Action<EtcdClientOptions> config = null)
    {
        var o = new EtcdClientOptions()
        {
            Address = options.Address,
        };
        config?.Invoke(o);
        services.UseEtcdClient();
        services.AddEtcdClient(nameof(EtcdProxyConfigSource), o);
        if (string.IsNullOrWhiteSpace(options.Prefix))
        {
            options.Prefix = "/ReverseProxy/";
        }
        services.AddSingleton(options);
        services.AddSingleton<IConfigSource<IProxyConfig>, EtcdProxyConfigSource>();
        return services;
    }

    public static IServiceCollection UseEtcdConfigFromEnv(this IServiceCollection services, Action<EtcdClientOptions> config = null)
    {
        return UseEtcdConfig(services, new EtcdProxyConfigSourceOptions()
        {
            Address = Environment.GetEnvironmentVariable("ETCD_CONNECTION_STRING").Split(",", StringSplitOptions.RemoveEmptyEntries),
            Prefix = Environment.GetEnvironmentVariable("ETCD_PREFIX"),
            Delay = TimeSpan.TryParse(Environment.GetEnvironmentVariable("ETCD_DELAY"), out var delay) ? delay : defaultDelay,
        }, config);
    }

    public static IServiceCollection UseEtcdConfig(this IServiceCollection services, IConfiguration configuration, string sectionKey = null, Action<EtcdClientOptions> config = null)
    {
        EtcdProxyConfigSourceOptions options = ConvertOptions(configuration, sectionKey);
        return UseEtcdConfig(services, options, config);
    }

    private static EtcdProxyConfigSourceOptions ConvertOptions(IConfiguration configuration, string sectionKey)
    {
        var options = new EtcdProxyConfigSourceOptions();
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            sectionKey = "Etcd";
        }
        var section = configuration.GetSection("Etcd");
        if (!section.Exists())
        {
            throw new ArgumentException($"Section {sectionKey} not found in configuration");
        }
        options.Address = section.GetSection(nameof(EtcdProxyConfigSourceOptions.Address)).ReadStringArray();
        options.Prefix = section[nameof(EtcdProxyConfigSourceOptions.Prefix)];
        options.Delay = section.ReadTimeSpan(nameof(EtcdProxyConfigSourceOptions.Delay)).GetValueOrDefault(defaultDelay);
        if (string.IsNullOrWhiteSpace(options.Prefix))
        {
            options.Prefix = "/ReverseProxy/";
        }
        return options;
    }

    public static IHostApplicationBuilder UseEtcdConfig(this IHostApplicationBuilder hostBuilder, string sectionKey = null, Action<EtcdClientOptions> config = null)
    {
        hostBuilder.Services.UseEtcdConfig(hostBuilder.Configuration, sectionKey, config);
        return hostBuilder;
    }
}
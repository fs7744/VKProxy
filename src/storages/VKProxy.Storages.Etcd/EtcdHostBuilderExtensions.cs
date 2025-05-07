using dotnet_etcd;
using dotnet_etcd.DependencyInjection;
using dotnet_etcd.interfaces;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using VKProxy.Config;
using VKProxy.Core.Config;

namespace VKProxy.Storages.Etcd;

public static class EtcdHostBuilderExtensions
{
    public static IServiceCollection UseEtcdConfig(this IServiceCollection services, EtcdProxyConfigSourceOptions options, Action<EtcdClientOptions> config = null)
    {
        var o = new EtcdClientOptions()
        {
            ConnectionString = options.ConnectionString,
            UseInsecureChannel = options.UseInsecureChannel,
        };
        config?.Invoke(o);
        services.AddEtcdClient(o);
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
            ConnectionString = Environment.GetEnvironmentVariable("ETCD_CONNECTION_STRING"),
            UseInsecureChannel = bool.TryParse(Environment.GetEnvironmentVariable("ETCD_USE_INSECURE_CHANNEL"), out var useInsecure) && useInsecure,
            Prefix = Environment.GetEnvironmentVariable("ETCD_PREFIX"),
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
        options.ConnectionString = section[nameof(EtcdProxyConfigSourceOptions.ConnectionString)];
        options.Prefix = section[nameof(EtcdProxyConfigSourceOptions.Prefix)];
        options.UseInsecureChannel = section.ReadBool(nameof(EtcdProxyConfigSourceOptions.UseInsecureChannel)).GetValueOrDefault();
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

    public static IHostBuilder UseEtcdConfig(this IHostBuilder hostBuilder, string sectionKey = null, Action<EtcdClientOptions> config = null)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.AddSingleton(i => ConvertOptions(i.GetRequiredService<IConfiguration>(), sectionKey));
            services.AddSingleton<IConfigSource<IProxyConfig>, EtcdProxyConfigSource>();
            services.TryAddSingleton((Func<IServiceProvider, IEtcdClient>)((IServiceProvider i) =>
            {
                var o = i.GetRequiredService<EtcdProxyConfigSourceOptions>();
                EtcdClientOptions options = new EtcdClientOptions
                {
                    ConnectionString = o.ConnectionString,
                    UseInsecureChannel = o.UseInsecureChannel,
                };
                config?.Invoke(options);
                return new EtcdClient(options.ConnectionString, options.Port, options.ServerName, oo =>
                {
                    if (options.UseInsecureChannel)
                    {
                        oo.Credentials = ChannelCredentials.Insecure;
                    }
                }, options.Interceptors);
            }));
            services.TryAddTransient((IServiceProvider serviceProvider) => (serviceProvider.GetRequiredService<IEtcdClient>() as EtcdClient)?.GetWatchManager());
        });
        return hostBuilder;
    }
}
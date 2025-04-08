using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VKProxy.Core.Adapters;
using VKProxy.Core.Hosting;
using VKProxy.Core.Loggers;

namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseVKProxyCore(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(services =>
        {
            services.UseInternalKestrel();
            services.AddSingleton<GeneralLogger>();
            services.AddSingleton<IHostedService, VKHostedService>();
            services.TryAddSingleton<IServer, VKServer>();
        });

        return hostBuilder;
    }
}
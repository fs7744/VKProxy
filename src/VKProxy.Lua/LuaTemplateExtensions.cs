using Lmzzz.AspNetCoreTemplate;
using Microsoft.Extensions.DependencyInjection;
using VKProxy;

namespace Microsoft.Extensions.Hosting;

public static class LuaTemplateExtensions
{
    public static IHostBuilder UseLuaTemplate(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureServices(i => i.UseLuaTemplate());
        return hostBuilder;
    }

    public static IHostApplicationBuilder UseLuaTemplate(this IHostApplicationBuilder hostBuilder)
    {
        hostBuilder.Services.UseLuaTemplate();
        return hostBuilder;
    }

    public static IServiceCollection UseLuaTemplate(this IServiceCollection services)
    {
        services.AddSingleton<ITemplateEngineFactory, TemplateEngineFactoryRoot>();
        return services;
    }
}

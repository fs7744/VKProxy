using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VKProxy.Features.Limits;
using VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

namespace VKProxy.StackExchangeRedis;

public static class StackExchangeRedisExtensions
{
    public static IRedisPool BuildPooledRedis(string configuration, int maxSize = 10)
    {
        ConfigurationOptions.Parse(configuration);
        return new RedisPool(async (i) => new AsyncPooledRedis(i, await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false)), maxSize);
    }

    public static IServiceCollection AddPooledRedis(this IServiceCollection services, string configuration, int maxSize = 10)
    {
        var pool = BuildPooledRedis(configuration, maxSize);
        services.AddSingleton<IRedisPool>(pool);

        return services;
    }

    public static IServiceCollection AddRedisResponseCache(this IServiceCollection services)
    {
        services.AddSingleton<IResponseCache, RedisResponseCache>();

        return services;
    }

    public static IServiceCollection AddRedisConcurrency(this IServiceCollection services)
    {
        services.AddSingleton<IConnectionLimitCreator, RedisIncrConnectionLimitCreator>();

        return services;
    }

    public static IServiceCollection PersistKeysToStackExchangeRedis(this IServiceCollection services, IRedisPool pool, string key = "DataProtection-Keys")
    {
        services.Configure<KeyManagementOptions>(options =>
        {
            options.XmlRepository = new RedisXmlRepository(pool, key);
        });
        return services;
    }
}
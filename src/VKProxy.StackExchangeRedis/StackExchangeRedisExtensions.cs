using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace VKProxy.StackExchangeRedis;

public static class StackExchangeRedisExtensions
{
    public static IServiceCollection AddPooledRedis(this IServiceCollection services, string configuration, int maxSize = 10)
    {
        ConfigurationOptions.Parse(configuration);
        if (maxSize <= 0)
        {
            throw new ArgumentException(nameof(maxSize));
        }
        services.AddSingleton<IRedisPool>(x =>
        {
            return new RedisPool(async (i) => new AsyncPooledRedis(i, await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false)), maxSize);
        });

        return services;
    }
}
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Features.Limits;

public class ConnectionLimiter : IConnectionLimiter
{
    private readonly ResourceCounter concurrentConnectionCounter;

    public ConnectionLimiter(long max)
    {
        concurrentConnectionCounter = ResourceCounter.Quota(max);
    }

    public IDecrementConcurrentConnectionCountFeature? TryLockOne(HttpContext context)
    {
        if (concurrentConnectionCounter.TryLockOne())
            return new ConnectionReleasor(concurrentConnectionCounter);
        else
            return null;
    }

    public IDecrementConcurrentConnectionCountFeature? TryLockOne(ConnectionContext connection)
    {
        if (concurrentConnectionCounter.TryLockOne())
            return new ConnectionReleasor(concurrentConnectionCounter);
        else
            return null;
    }
}
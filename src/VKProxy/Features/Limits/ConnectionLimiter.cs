using VKProxy.Core.Infrastructure;

namespace VKProxy.Features.Limits;

public class ConnectionLimiter : IConnectionLimiter
{
    private readonly ResourceCounter concurrentConnectionCounter;

    public ConnectionLimiter(long max)
    {
        concurrentConnectionCounter = ResourceCounter.Quota(max);
    }

    public IDecrementConcurrentConnectionCountFeature? TryLockOne()
    {
        if (concurrentConnectionCounter.TryLockOne())
            return new ConnectionReleasor(concurrentConnectionCounter);
        else
            return null;
    }
}
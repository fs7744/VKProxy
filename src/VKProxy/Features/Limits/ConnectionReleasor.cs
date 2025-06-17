using VKProxy.Core.Infrastructure;

namespace VKProxy.Features.Limits;

public sealed class ConnectionReleasor : IDecrementConcurrentConnectionCountFeature
{
    private readonly ResourceCounter concurrentConnectionCounter;
    private bool connectionReleased;

    public ConnectionReleasor(ResourceCounter normalConnectionCounter)
    {
        concurrentConnectionCounter = normalConnectionCounter;
    }

    public void ReleaseConnection()
    {
        if (!connectionReleased)
        {
            connectionReleased = true;
            concurrentConnectionCounter.ReleaseOne();
        }
    }
}
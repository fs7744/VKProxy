namespace VKProxy.Features.Limits;

public interface IConnectionLimiter
{
    public IDecrementConcurrentConnectionCountFeature? TryLockOne();
}

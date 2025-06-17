using VKProxy.Config;

namespace VKProxy.Features.Limits;

public interface IConnectionLimitFactory
{
    public IConnectionLimiter? Default { get; }

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options);
}
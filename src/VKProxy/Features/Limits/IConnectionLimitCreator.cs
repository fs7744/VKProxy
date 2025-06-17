namespace VKProxy.Features.Limits;

public interface IConnectionLimitCreator
{
    public string Name { get; }

    public IConnectionLimiter? Create(ConcurrentConnectionLimitOptions options);
}

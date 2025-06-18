using System.Threading.RateLimiting;

namespace VKProxy.Features.Limits;

public interface IConnectionLimiter
{
    RateLimiter? GetLimiter(IReverseProxyFeature proxyFeature);
}

public class ConnectionLimiter : IConnectionLimiter
{
    private readonly RateLimiter rateLimiter;

    public ConnectionLimiter(RateLimiter rateLimiter)
    {
        this.rateLimiter = rateLimiter;
    }

    public RateLimiter? GetLimiter(IReverseProxyFeature proxyFeature)
    {
        return rateLimiter;
    }
}
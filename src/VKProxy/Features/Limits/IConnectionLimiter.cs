using System.Threading.RateLimiting;

namespace VKProxy.Features.Limits;

public interface IConnectionLimiter
{
    RateLimiter? GetLimiter(IReverseProxyFeature proxyFeature);

    IEnumerable<KeyValuePair<string, RateLimiter>> GetAllLimiter();
}

public class ConnectionLimiter : IConnectionLimiter
{
    private readonly RateLimiter rateLimiter;

    public ConnectionLimiter(RateLimiter rateLimiter)
    {
        this.rateLimiter = rateLimiter;
    }

    private KeyValuePair<string, RateLimiter>[] rateLimiters;

    public IEnumerable<KeyValuePair<string, RateLimiter>> GetAllLimiter()
    {
        if (rateLimiters == null)
        {
            rateLimiters = new[] { new KeyValuePair<string, RateLimiter>("route", rateLimiter) };
        }
        return rateLimiters;
    }

    public RateLimiter? GetLimiter(IReverseProxyFeature proxyFeature)
    {
        return rateLimiter;
    }
}
using Microsoft.AspNetCore.Connections;

namespace VKProxy.Features.Limits;

public interface IConnectionLimiter
{
    IDecrementConcurrentConnectionCountFeature? TryLockOne(Microsoft.AspNetCore.Http.HttpContext context);

    IDecrementConcurrentConnectionCountFeature? TryLockOne(ConnectionContext connection);
}
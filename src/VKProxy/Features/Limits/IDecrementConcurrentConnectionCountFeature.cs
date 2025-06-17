namespace VKProxy.Features;

public interface IDecrementConcurrentConnectionCountFeature
{
    void ReleaseConnection();
}
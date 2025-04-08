namespace VKProxy.Core.Adapters;

public interface IHeartbeat
{
    void StartHeartbeat();

    void StopHeartbeat();
}
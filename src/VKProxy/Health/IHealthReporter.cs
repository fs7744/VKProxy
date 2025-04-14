using VKProxy.Config;

namespace VKProxy.Health;

public interface IHealthReporter
{
    void ReportFailed(DestinationState destinationState);

    void ReportSuccessed(DestinationState destinationState);
}
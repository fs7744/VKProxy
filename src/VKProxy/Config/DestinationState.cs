using System.Net;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Config;

public class DestinationState : IDisposable
{
    public EndPoint? EndPoint { get; set; }

    public int ConcurrentRequestCount
    {
        get => ConcurrencyCounter.Value;
        set => ConcurrencyCounter.Value = value;
    }

    internal AtomicCounter ConcurrencyCounter { get; } = new AtomicCounter();

    internal ClusterConfig ClusterConfig { get; set; }

    public DestinationHealth Health { get; set; }
    public string? Host { get; set; }

    public void Dispose()
    {
        ClusterConfig = null;
    }

    internal void ReportFailed()
    {
        ClusterConfig?.HealthReporter?.ReportFailed(this);
    }

    internal void ReportSuccessed()
    {
        ClusterConfig?.HealthReporter?.ReportSuccessed(this);
    }
}
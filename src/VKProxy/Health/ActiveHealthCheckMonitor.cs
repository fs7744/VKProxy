using System.Collections.Frozen;
using VKProxy.Config;
using VKProxy.Core.Infrastructure;
using VKProxy.Core.Loggers;

namespace VKProxy.Health;

public class ActiveHealthCheckMonitor : IActiveHealthCheckMonitor, IDisposable
{
    private readonly FrozenDictionary<string, IActiveHealthChecker> checkers;
    private readonly IHealthUpdater healthUpdater;
    private readonly ProxyLogger logger;
    private readonly CancellationTokenSourcePool cancellationTokenSourcePool = new();

    public ActiveHealthCheckMonitor(TimeProvider timeProvider, IEnumerable<IActiveHealthChecker> checkers, IHealthUpdater healthUpdater, ProxyLogger logger)
    {
        Scheduler = new EntityActionScheduler<WeakReference<ClusterConfig>>(ProbeCluster, autoStart: false, runOnce: false, timeProvider);
        this.checkers = checkers.ToFrozenDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        this.healthUpdater = healthUpdater;
        this.logger = logger;
    }

    private async Task ProbeCluster(WeakReference<ClusterConfig> reference)
    {
        if (!reference.TryGetTarget(out var cluster) || cluster.HealthCheck is null || cluster.HealthCheck.Active is null)
        {
            Scheduler.UnscheduleEntity(reference);
            return;
        }

        var config = cluster.HealthCheck.Active;
        if (!checkers.TryGetValue(config.Policy, out var checker))
        {
            logger.NotFoundActiveHealthCheckPolicy(config.Policy);
            Scheduler.UnscheduleEntity(reference);
            return;
        }

        try
        {
            var cts = cancellationTokenSourcePool.Rent();
            cts.CancelAfter(config.Timeout);
            var all = cluster.DestinationStates.ToArray();
            await Task.WhenAll(all.Select(i => checker.CheckAsync(config, i, cts.Token)).ToArray());
            healthUpdater.UpdateAvailableDestinations(cluster);
        }
        catch (Exception ex)
        {
            logger.UnexpectedException(nameof(ActiveHealthCheckMonitor), ex);
        }
    }

    internal EntityActionScheduler<WeakReference<ClusterConfig>> Scheduler { get; }

    public Task CheckHealthAsync(IEnumerable<ClusterConfig> clusters)
    {
        return Task.Run(async () =>
        {
            try
            {
                var probeClusterTasks = new List<Task>();
                foreach (var cluster in clusters)
                {
                    if (cluster.HealthCheck?.Active != null)
                    {
                        var r = new WeakReference<ClusterConfig>(cluster);
                        Scheduler.ScheduleEntity(r, cluster.HealthCheck.Active.Interval);
                        probeClusterTasks.Add(ProbeCluster(r));
                    }
                }

                await Task.WhenAll(probeClusterTasks);
            }
            catch (Exception ex)
            {
                logger.UnexpectedException(nameof(ActiveHealthCheckMonitor), ex);
            }

            Scheduler.Start();
        });
    }

    public void Dispose()
    {
        Scheduler.Dispose();
    }
}
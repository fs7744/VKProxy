using System.Runtime.CompilerServices;
using VKProxy.Config;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Health;

public class PassiveHealthReporter : IHealthReporter
{
    private readonly ConditionalWeakTable<DestinationState, ProxiedRequestHistory> _requestHistories = new ConditionalWeakTable<DestinationState, ProxiedRequestHistory>();
    private readonly TimeProvider timeProvider;
    private readonly EntityActionScheduler<DestinationState> scheduler;
    private IHealthUpdater updater;

    public PassiveHealthReporter(TimeProvider timeProvider, IHealthUpdater healthUpdater)
    {
        this.timeProvider = timeProvider;
        this.updater = healthUpdater;
        scheduler = new EntityActionScheduler<DestinationState>(Reactivate, autoStart: true, runOnce: true, timeProvider);
    }

    private Task Reactivate(DestinationState d)
    {
        if (d.Health == DestinationHealth.Unhealthy)
        {
            d.Health = DestinationHealth.Unknown;
            updater.UpdateAvailableDestinations(d.ClusterConfig);
        }

        return Task.CompletedTask;
    }

    public void ReportFailed(DestinationState destinationState)
    {
        Update(destinationState, true);
    }

    private void Update(DestinationState destinationState, bool isFailed)
    {
        var options = destinationState.ClusterConfig.HealthCheck.Passive;
        var history = _requestHistories.GetOrCreateValue(destinationState);
        DestinationHealth newHealth;
        lock (history)
        {
            var failureRate = history.AddNew(
                timeProvider,
                options.DetectionWindowSize,
                options.MinimalTotalCountThreshold,
                isFailed);
            newHealth = failureRate < options.FailureRateLimit ? DestinationHealth.Healthy : DestinationHealth.Unhealthy;
        }
        if (destinationState.Health != newHealth)
        {
            destinationState.Health = newHealth;
            if (newHealth == DestinationHealth.Unhealthy)
            {
                Task.Factory.StartNew(c => UpdateDestinations(c!), destinationState.ClusterConfig, CancellationToken.None, TaskCreationOptions.RunContinuationsAsynchronously, TaskScheduler.Default);
                scheduler.ScheduleEntity(destinationState, options.ReactivationPeriod);
            }
        }
    }

    private void UpdateDestinations(object value)
    {
        updater.UpdateAvailableDestinations((ClusterConfig)value);
    }

    public void ReportSuccessed(DestinationState destinationState)
    {
        Update(destinationState, false);
    }

    private sealed class ProxiedRequestHistory
    {
        private long _nextRecordCreatedAt;
        private long _nextRecordTotalCount;
        private long _nextRecordFailedCount;
        private long _failedCount;
        private double _totalCount;
        private readonly Queue<HistoryRecord> _records = new Queue<HistoryRecord>();

        public double AddNew(TimeProvider timeProvider, TimeSpan detectionWindowSize, int totalCountThreshold, bool failed)
        {
            var eventTime = timeProvider.GetTimestamp();
            var detectionWindowSizeLong = detectionWindowSize.TotalSeconds * timeProvider.TimestampFrequency;
            if (_nextRecordCreatedAt == 0)
            {
                // Initialization.
                _nextRecordCreatedAt = eventTime + timeProvider.TimestampFrequency;
            }

            // Don't create a new record on each event because it can negatively affect performance.
            // Instead, accumulate failed and total request counts reported during some period
            // and then add only one record storing them.
            if (eventTime >= _nextRecordCreatedAt)
            {
                _records.Enqueue(new HistoryRecord(_nextRecordCreatedAt, _nextRecordTotalCount, _nextRecordFailedCount));
                _nextRecordCreatedAt = eventTime + timeProvider.TimestampFrequency;
                _nextRecordTotalCount = 0;
                _nextRecordFailedCount = 0;
            }

            _nextRecordTotalCount++;
            _totalCount++;
            if (failed)
            {
                _failedCount++;
                _nextRecordFailedCount++;
            }

            while (_records.Count > 0 && (eventTime - _records.Peek().RecordedAt > detectionWindowSizeLong))
            {
                var removed = _records.Dequeue();
                _failedCount -= removed.FailedCount;
                _totalCount -= removed.TotalCount;
            }

            return _totalCount < totalCountThreshold || _totalCount == 0 ? 0.0 : _failedCount / _totalCount;
        }

        private readonly struct HistoryRecord
        {
            public HistoryRecord(long recordedAt, long totalCount, long failedCount)
            {
                RecordedAt = recordedAt;
                TotalCount = totalCount;
                FailedCount = failedCount;
            }

            public long RecordedAt { get; }

            public long TotalCount { get; }

            public long FailedCount { get; }
        }
    }
}
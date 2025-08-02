namespace VKProxy.Telemetry;

/// <summary>
/// A consumer of <typeparamref name="TMetrics"/>.
/// </summary>
public interface IMetricsConsumer<TMetrics>
{
    /// <summary>
    /// Processes <typeparamref name="TMetrics"/> from the last event counter interval.
    /// </summary>
    /// <param name="previous"><typeparamref name="TMetrics"/> collected in the previous interval.</param>
    /// <param name="current"><typeparamref name="TMetrics"/> collected in the last interval.</param>
    void OnMetrics(TMetrics previous, TMetrics current);
}
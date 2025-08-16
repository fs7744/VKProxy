using VKProxy.Kubernetes.Controller.Rate;

namespace VKProxy.Kubernetes.Controller.Queues;

public class ProcessingRateLimitedQueue<TItem> : WorkQueue<TItem>
{
    private readonly Limiter _limiter;

    public ProcessingRateLimitedQueue(double perSecond, int burst)
    {
        _limiter = new Limiter(new Limit(perSecond), burst);
    }

    protected override async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var delay = _limiter.Reserve().Delay();
        await Task.Delay(delay, cancellationToken);
    }
}
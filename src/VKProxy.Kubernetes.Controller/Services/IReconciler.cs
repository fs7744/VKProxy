namespace VKProxy.Kubernetes.Controller.Services;

public interface IReconciler
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
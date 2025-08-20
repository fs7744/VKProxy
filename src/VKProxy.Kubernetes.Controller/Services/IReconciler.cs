namespace VKProxy.Kubernetes.Controller.Services;

public interface IReconciler
{
    Task ProcessAsync(IEnumerable<IK8SChange> changes, CancellationToken cancellationToken);
}
namespace VKProxy.Kubernetes.Controller.Client;

public interface IIngressResourceStatusUpdater
{
    /// <summary>
    /// Updates the status of cached ingresses.
    /// </summary>
    Task UpdateStatusAsync(CancellationToken cancellationToken);
}
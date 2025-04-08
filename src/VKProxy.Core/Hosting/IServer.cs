namespace VKProxy.Core.Hosting;

public interface IServer
{
    public Task StartAsync(CancellationToken cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken);
}
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Net;
using VKProxy.Core.Adapters;
using VKProxy.Core.Config;
using VKProxy.Core.Hosting;

namespace CoreDemo;

internal class TcpListenHandler : ListenHandlerBase
{
    private readonly List<EndPointOptions> endPointOptions = new List<EndPointOptions>();
    private readonly ILogger<TcpListenHandler> logger;
    private readonly IConnectionFactory connectionFactory;

    public TcpListenHandler(ILogger<TcpListenHandler> logger, IConnectionFactory connectionFactory)
    {
        this.logger = logger;
        this.connectionFactory = connectionFactory;
    }

    public override Task InitAsync(CancellationToken cancellationToken)
    {
        endPointOptions.Add(new EndPointOptions()
        {
            EndPoint = IPEndPoint.Parse("127.0.0.1:5000"),
            Key = "tcp"
        });
        return Task.CompletedTask;
    }

    public override async Task BindAsync(ITransportManager transportManager, CancellationToken cancellationToken)
    {
        foreach (var item in endPointOptions)
        {
            try
            {
                await transportManager.BindAsync(item, Proxy, cancellationToken);
                logger.LogInformation($"listen {item.EndPoint}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
        }
    }

    private async Task Proxy(ConnectionContext connection)
    {
        logger.LogInformation($"begin tcp {DateTime.Now} {connection.LocalEndPoint.ToString()} ");
        var upstream = await connectionFactory.ConnectAsync(new IPEndPoint(IPAddress.Parse("14.215.177.38"), 80));
        var task1 = connection.Transport.Input.CopyToAsync(upstream.Transport.Output);
        var task2 = upstream.Transport.Input.CopyToAsync(connection.Transport.Output);
        await Task.WhenAny(task1, task2);
        upstream.Abort();
        connection.Abort();
        logger.LogInformation($"end tcp {DateTime.Now} {connection.LocalEndPoint.ToString()} ");
    }
}
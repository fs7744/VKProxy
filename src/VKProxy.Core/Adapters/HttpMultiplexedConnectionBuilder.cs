using Microsoft.AspNetCore.Connections;

namespace VKProxy.Core.Adapters;

public class HttpMultiplexedConnectionBuilder : IMultiplexedConnectionBuilder
{
    private readonly List<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>> multiplexedMiddlewares = new List<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>>();

    public HttpMultiplexedConnectionBuilder(IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
    }

    public IServiceProvider ApplicationServices { get; private set; }

    public MultiplexedConnectionDelegate Build()
    {
        MultiplexedConnectionDelegate app = context =>
        {
            return Task.CompletedTask;
        };

        for (int i = multiplexedMiddlewares.Count - 1; i >= 0; i--)
        {
            var component = multiplexedMiddlewares[i];
            app = component(app);
        }

        return app;
    }

    public IMultiplexedConnectionBuilder Use(Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate> middleware)
    {
        multiplexedMiddlewares.Add(middleware);
        return this;
    }
}
using Microsoft.AspNetCore.Connections;

namespace VKProxy.Core.Adapters;

public class HttpConnectionBuilder : IConnectionBuilder
{
    private readonly List<Func<ConnectionDelegate, ConnectionDelegate>> middlewares = new List<Func<ConnectionDelegate, ConnectionDelegate>>();

    public HttpConnectionBuilder(IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
    }

    public IServiceProvider ApplicationServices { get; private set; }

    public ConnectionDelegate Build()
    {
        ConnectionDelegate app = context =>
        {
            return Task.CompletedTask;
        };

        for (var i = middlewares.Count - 1; i >= 0; i--)
        {
            var component = middlewares[i];
            app = component(app);
        }

        return app;
    }

    public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware)
    {
        middlewares.Add(middleware);
        return this;
    }
}
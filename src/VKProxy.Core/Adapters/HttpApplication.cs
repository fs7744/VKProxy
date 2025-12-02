using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace VKProxy.Core.Adapters;

public class HttpApplication : IHttpApplication<HttpApplication.Context>
{
    private readonly RequestDelegate application;
    private readonly IHttpContextFactory httpContextFactory;
    private readonly DefaultHttpContextFactory? defaultHttpContextFactory;

    public HttpApplication(RequestDelegate application, IHttpContextFactory httpContextFactory)
    {
        this.application = application;
        if (httpContextFactory is DefaultHttpContextFactory factory)
        {
            defaultHttpContextFactory = factory;
        }
        else
        {
            this.httpContextFactory = httpContextFactory;
        }
    }

    public Context CreateContext(IFeatureCollection contextFeatures)
    {
        Context? hostContext;
        if (contextFeatures is IHostContextContainer<Context> container)
        {
            hostContext = container.HostContext;
            if (hostContext is null)
            {
                hostContext = new Context();
                container.HostContext = hostContext;
            }
        }
        else
        {
            // Server doesn't support pooling, so create a new Context
            hostContext = new Context();
        }

        HttpContext httpContext;
        if (defaultHttpContextFactory != null)
        {
            var defaultHttpContext = (DefaultHttpContext?)hostContext.HttpContext;
            if (defaultHttpContext is null)
            {
                httpContext = defaultHttpContextFactory.Create(contextFeatures);
                hostContext.HttpContext = httpContext;
            }
            else
            {
                defaultHttpContextFactory.Initialize(defaultHttpContext, contextFeatures);
                httpContext = defaultHttpContext;
            }
        }
        else
        {
            httpContext = httpContextFactory!.Create(contextFeatures);
            hostContext.HttpContext = httpContext;
        }

        return hostContext;
    }

    public void DisposeContext(Context context, Exception? exception)
    {
        var httpContext = context.HttpContext!;

        if (defaultHttpContextFactory != null)
        {
            defaultHttpContextFactory.Dispose((DefaultHttpContext)httpContext);

            if (defaultHttpContextFactory.HttpContextAccessor != null)
            {
                // Clear the HttpContext if the accessor was used. It's likely that the lifetime extends
                // past the end of the http request and we want to avoid changing the reference from under
                // consumers.
                context.HttpContext = null;
            }
        }
        else
        {
            httpContextFactory!.Dispose(httpContext);
        }

        // Reset the context as it may be pooled
        context.Reset();
    }

    public Task ProcessRequestAsync(Context context)
    {
        return application(context.HttpContext!);
    }

    public sealed class Context
    {
        public HttpContext? HttpContext { get; set; }
        public IDisposable? Scope { get; set; }

        public long StartTimestamp { get; set; }

        public void Reset()
        {
            Scope = null;
            StartTimestamp = 0;
        }
    }
}
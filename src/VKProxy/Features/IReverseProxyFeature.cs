using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using VKProxy.Config;

namespace VKProxy.Features;

public interface IL4ReverseProxyFeature : IReverseProxyFeature
{
    public bool IsDone { get; set; }
    public bool IsSni { get; set; }
    public ConnectionContext Connection { get; set; }
}

public interface IReverseProxyFeature
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }
    public long StartTimestamp { get; set; }
    public Activity? Activity { get; set; }
}

public interface IL7ReverseProxyFeature : IReverseProxyFeature
{
    public HttpContext Http { get; set; }
}

public class L4ReverseProxyFeature : IL4ReverseProxyFeature, IDisposable
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }

    public bool IsDone { get; set; }
    public bool IsSni { get; set; }
    public SniConfig? SelectedSni { get; set; }
    public ConnectionContext Connection { get; set; }
    public long StartTimestamp { get; set; }
    public Activity? Activity { get; set; }

    public void Dispose()
    {
        Route = null;
        Connection = null;
        SelectedDestination = null;
    }
}

public class L7ReverseProxyFeature : IL7ReverseProxyFeature, IDisposable
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }
    public HttpContext Http { get; set; }
    public long StartTimestamp { get; set; }
    public Activity? Activity { get; set; }

    public void Dispose()
    {
        Route = null;
        Http = null;
        SelectedDestination = null;
    }
}
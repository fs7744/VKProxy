﻿using VKProxy.Config;

namespace VKProxy.Features;

public interface IL4ReverseProxyFeature : IReverseProxyFeature
{
    public bool IsDone { get; set; }
    public bool IsSni { get; set; }
    public SniConfig? SelectedSni { get; set; }
}

public interface IReverseProxyFeature
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }
}

public class L4ReverseProxyFeature : IL4ReverseProxyFeature
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }

    public bool IsDone { get; set; }
    public bool IsSni { get; set; }
    public SniConfig? SelectedSni { get; set; }
}

public class L7ReverseProxyFeature : IReverseProxyFeature
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }
}
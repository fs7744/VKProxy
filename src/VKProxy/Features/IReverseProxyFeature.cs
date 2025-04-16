﻿using VKProxy.Config;

namespace VKProxy.Features;

public interface IReverseProxyFeature
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }
    public bool IsDone { get; set; }
    public bool IsSni { get; set; }
    public SniConfig? SelectedSni { get; set; }
}

public class ReverseProxyFeature : IReverseProxyFeature
{
    public RouteConfig Route { get; set; }
    public DestinationState? SelectedDestination { get; set; }

    public bool IsDone { get; set; }
    public bool IsSni { get; set; }
    public SniConfig? SelectedSni { get; set; }
}
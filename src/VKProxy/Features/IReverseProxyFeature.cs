﻿using VKProxy.Config;

namespace VKProxy.Features;

public interface IReverseProxyFeature
{
    public RouteConfig Route { get; set; }
}

public class ReverseProxyFeature : IReverseProxyFeature
{
    public RouteConfig Route { get; set; }
}
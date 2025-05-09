﻿using Microsoft.AspNetCore.Http;
using VKProxy.Config;
using VKProxy.Core.Loggers;
using VKProxy.Features;
using VKProxy.LoadBalancing;

namespace VKProxy.Middlewares.Http;

public class HttpReverseProxy : IMiddleware
{
    private readonly ILoadBalancingPolicyFactory loadBalancing;
    private readonly ProxyLogger logger;
    private readonly IHttpForwarder forwarder;

    public HttpReverseProxy(ILoadBalancingPolicyFactory loadBalancing, ProxyLogger logger, IHttpForwarder forwarder)
    {
        this.loadBalancing = loadBalancing;
        this.logger = logger;
        this.forwarder = forwarder;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var resp = context.Response;
        if (resp.HasStarted) return;
        var proxyFeature = context.Features.Get<IReverseProxyFeature>();
        if (proxyFeature is not null)
        {
            var route = proxyFeature.Route;

            if (route is not null)
            {
                var cluster = route.ClusterConfig;
                DestinationState selectedDestination;
                if (cluster is null)
                {
                    selectedDestination = null;
                }
                else
                {
                    selectedDestination = proxyFeature.SelectedDestination;
                    selectedDestination ??= loadBalancing.PickDestination(proxyFeature);
                }

                if (selectedDestination is null)
                {
                    logger.NotFoundAvailableUpstream(route.ClusterId);
                    resp.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return;
                }
                selectedDestination.ConcurrencyCounter.Increment();
                try
                {
                    await forwarder.SendAsync(context, proxyFeature, selectedDestination, cluster, route.Transformer);
                    if (DestinationFailed(context))
                    {
                        selectedDestination.ReportFailed();
                    }
                    else
                    {
                        selectedDestination.ReportSuccessed();
                    }
                }
                finally
                {
                    selectedDestination.ConcurrencyCounter.Decrement();
                }

                return;
            }
        }

        resp.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    private static bool DestinationFailed(HttpContext context)
    {
        var errorFeature = context.Features.Get<IForwarderErrorFeature>();
        if (errorFeature is null)
        {
            return false;
        }

        if (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected/canceled the request - the failure may not be the destination's fault
            return false;
        }

        var error = errorFeature.Error;

        return error == ForwarderError.Request
            || error == ForwarderError.RequestTimedOut
            || error == ForwarderError.RequestBodyDestination
            || error == ForwarderError.ResponseBodyDestination
            || error == ForwarderError.UpgradeRequestDestination
            || error == ForwarderError.UpgradeResponseDestination;
    }
}
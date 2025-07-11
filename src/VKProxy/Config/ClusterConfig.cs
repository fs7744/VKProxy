﻿using VKProxy.Core.Infrastructure;
using VKProxy.Health;
using VKProxy.LoadBalancing;
using VKProxy.Middlewares.Http;

namespace VKProxy.Config;

public class ClusterConfig
{
    public string Key { get; set; }

    public string? LoadBalancingPolicy { get; set; }

    public HealthCheckConfig? HealthCheck { get; set; }

    public IReadOnlyList<DestinationConfig>? Destinations { get; set; }

    public HttpClientConfig? HttpClientConfig { get; set; }
    public ForwarderRequestConfig? HttpRequest { get; set; }

    internal IReadOnlyList<DestinationState> DestinationStates { get; set; }

    internal ILoadBalancingPolicy LoadBalancingPolicyInstance { get; set; }
    internal List<DestinationState> AvailableDestinations { get; set; }
    internal IHealthReporter HealthReporter { get; set; }
    internal HttpMessageInvoker HttpMessageHandler { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public void Dispose()
    {
        DestinationStates = null;
        LoadBalancingPolicyInstance = null;
    }

    public static bool Equals(ClusterConfig? t, ClusterConfig? other)
    {
        if (other is null)
        {
            return t is null;
        }

        if (t is null)
        {
            return other is null;
        }

        return string.Equals(t.Key, other.Key, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.LoadBalancingPolicy, other.LoadBalancingPolicy, StringComparison.OrdinalIgnoreCase)
            && HealthCheckConfig.Equals(t.HealthCheck, other.HealthCheck)
            && CollectionUtilities.Equals(t.Destinations, other.Destinations, DestinationConfig.Comparer)
            && HttpClientConfig.Equals(t.HttpClientConfig, other.HttpClientConfig)
            && t.HttpRequest?.Equals(other.HttpRequest) == true
            && CollectionUtilities.Equals(t.Metadata, other.Metadata);
    }

    public override bool Equals(object? obj)
    {
        return obj is ClusterConfig o && Equals(this, o);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Key?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            LoadBalancingPolicy?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            HealthCheck,
            CollectionUtilities.GetHashCode(Destinations),
            HttpClientConfig);
    }

    internal void InitHttp(IForwarderHttpClientFactory httpClientFactory)
    {
        if (HttpMessageHandler == null)
        {
            HttpMessageHandler = httpClientFactory.CreateHttpClient(HttpClientConfig);
        }
    }
}
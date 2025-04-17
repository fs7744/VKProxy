using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Collections;
using System.Net;
using VKProxy.Core.Adapters;

namespace VKProxy.Core.Config;

public class EndPointOptions
{
    public string Key { get; set; }

    public EndPoint EndPoint { get; set; }

    private object EndpointConfig;

    private ListenOptions ListenOptions;

    public static bool Equals(EndPointOptions? t, EndPointOptions? other)
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
            && other.EndPoint.GetHashCode() == other.EndPoint.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is EndPointOptions o && Equals(this, o);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key?.GetHashCode(StringComparison.OrdinalIgnoreCase), EndPoint.GetHashCode());
    }

    public override string ToString()
    {
        return $"[Key: {Key},EndPoint: {EndPoint}]";
    }

    public ListenOptions GetListenOptions()
    {
        if (ListenOptions is null)
        {
            ListenOptions = KestrelExtensions.InitListenOptions(EndPoint, Init());
        }
        return ListenOptions;
    }

    public void SetHttpsCallbackOptions(TlsHandshakeCallbackOptions callbackOptions)
    {
        var o = GetListenOptions();
        KestrelExtensions.ListenOptionsSetHttpsCallbackOptions.Invoke(o, new object[] { callbackOptions });
    }

    public void SetHttpsOptions(HttpsConnectionAdapterOptions callbackOptions)
    {
        var o = GetListenOptions();
        KestrelExtensions.ListenOptionsSetHttpsOptions.Invoke(o, new object[] { callbackOptions });
    }

    internal object Init()
    {
        if (EndpointConfig is null)
        {
            EndpointConfig = KestrelExtensions.InitEndpointConfig(Key, null, NothingConfigurationSection.Nothing);
        }
        return EndpointConfig;
    }

    internal static object Init(List<EndPointOptions> endpointsToStop)
    {
        var endpoints = KestrelExtensions.EndpointConfigInitListMethod.Invoke(null) as IList;
        foreach (EndPointOptions endpoint in endpointsToStop)
        {
            endpoints.Add(endpoint.Init());
        }
        return endpoints;
    }
}
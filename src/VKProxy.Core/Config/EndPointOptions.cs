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

    public virtual bool Equals(EndPointOptions? obj)
    {
        if (obj is null) return false;
        return Key.Equals(obj.Key, StringComparison.OrdinalIgnoreCase)
            && EndPoint.GetHashCode() == EndPoint.GetHashCode();
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
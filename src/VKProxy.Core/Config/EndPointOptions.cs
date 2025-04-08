using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using System.Collections;
using System.Net;
using System.Reflection;

namespace VKProxy.Core.Config;

public class EndPointOptions
{
    private static readonly Type typeEndpointConfig;
    private static readonly ConstructorInfo initMethod;
    private static readonly ConstructorInfo initListMethod;

    public string Key { get; set; }

    public EndPoint EndPoint { get; set; }

    internal object EndpointConfig { get; set; }

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
        return $"Key: {Key},EndPoint: {EndPoint}]";
    }

    static EndPointOptions()
    {
        var types = typeof(KestrelServer).Assembly.GetTypes();
        typeEndpointConfig = types.First(i => i.Name == "EndpointConfig");
        initMethod = typeEndpointConfig.GetTypeInfo().DeclaredConstructors.First();
        var list = typeof(List<>).MakeGenericType(typeEndpointConfig).GetTypeInfo();
        initListMethod = list.DeclaredConstructors.First(i => i.GetParameters().Length == 0);
    }

    private static object InitEndpointConfig(string key, string url, IConfigurationSection section)
    {
        return initMethod.Invoke(new object[] { key, url, null, section });
    }

    internal object Init()
    {
        if (EndpointConfig is null)
        {
            EndpointConfig = InitEndpointConfig(Key, null, NothingConfigurationSection.Nothing);
        }
        return EndpointConfig;
    }

    internal static object Init(List<EndPointOptions> endpointsToStop)
    {
        var endpoints = initListMethod.Invoke(null) as IList;
        foreach (EndPointOptions endpoint in endpointsToStop)
        {
            endpoints.Add(endpoint.Init());
        }
        return endpoints;
    }
}
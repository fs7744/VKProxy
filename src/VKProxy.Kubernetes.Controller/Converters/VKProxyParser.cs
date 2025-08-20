using k8s.Models;
using System.Globalization;
using System.Runtime.InteropServices;
using VKProxy.Config;
using VKProxy.Features.Limits;
using VKProxy.HttpRoutingStatement;
using VKProxy.Kubernetes.Controller.Caching;
using YamlDotNet.Serialization;

namespace VKProxy.Kubernetes.Controller.Converters;

public static class VKProxyParser
{
    private static readonly string[] AnyHosts = new[] { "*" };
    private const string ExternalNameServiceType = "ExternalName";
    private static readonly Deserializer YamlDeserializer = new();

    public static void ConvertFromKubernetesIngress(VKProxyIngressContext ingressContext, VKProxyConfigContext configContext)
    {
        var spec = ingressContext.Ingress.Spec;
        var defaultBackend = spec?.DefaultBackend;
        var defaultService = defaultBackend?.Service;
        IList<V1EndpointSubset> defaultSubsets = default;

        if (!string.IsNullOrEmpty(defaultService?.Name))
        {
            defaultSubsets = ingressContext.Endpoints.SingleOrDefault(x => x.Name == defaultService?.Name).Subsets;
        }

        HandleAnnotations(ingressContext, ingressContext.Ingress.Metadata);

        foreach (var rule in spec?.Rules ?? Enumerable.Empty<V1IngressRule>())
        {
            HandleIngressRule(ingressContext, ingressContext.Endpoints, defaultSubsets, rule, configContext);
        }
    }

    private static void HandleIngressRule(VKProxyIngressContext ingressContext, List<Endpoints> endpoints, IList<V1EndpointSubset>? defaultSubsets, V1IngressRule rule, VKProxyConfigContext configContext)
    {
        var http = rule.Http;
        foreach (var path in http.Paths ?? Enumerable.Empty<V1HTTPIngressPath>())
        {
            var service = ingressContext.Services.SingleOrDefault(s => s.Metadata.Name == path.Backend.Service.Name);
            if (service.Spec != null)
            {
                if (string.Equals(service.Spec.Type, ExternalNameServiceType, StringComparison.OrdinalIgnoreCase))
                {
                    HandleExternalIngressRulePath(ingressContext, service.Spec.ExternalName, rule, path, configContext);
                }
                else
                {
                    var servicePort = service.Spec.Ports.SingleOrDefault(p => MatchesPort(p, path.Backend.Service.Port));
                    if (servicePort != null)
                    {
                        //todo
                        //HandleIngressRulePath(ingressContext, servicePort, endpoints, defaultSubsets, rule, path, configContext);
                    }
                }
            }
        }
    }

    private static void HandleExternalIngressRulePath(VKProxyIngressContext ingressContext, string externalName, V1IngressRule rule, V1HTTPIngressPath path, VKProxyConfigContext configContext)
    {
        var backend = path.Backend;
        var ingressServiceBackend = backend.Service;

        var cluster = GetOrAddCluster(ingressContext, configContext, ingressServiceBackend);
        var route = CreateRoute(ingressContext, path, cluster, rule.Host);
        configContext.Routes[route.Key] = route;
        AddDestination(cluster, ingressContext, externalName, ingressServiceBackend.Port.Number);
    }

    private static void AddDestination(ClusterConfig cluster, VKProxyIngressContext ingressContext, string host, int? port)
    {
        var isHttps =
            ingressContext.Options.Https ||
            cluster.Key.EndsWith(":443", StringComparison.Ordinal) ||
            cluster.Key.EndsWith(":https", StringComparison.OrdinalIgnoreCase);

        var protocol = isHttps ? "https" : "http";

        var uri = $"{protocol}://{host}";
        if (port.HasValue)
        {
            uri += $":{port}";
        }
        cluster.Destinations.Add(new DestinationConfig()
        {
            Address = uri,
        });
    }

    private static RouteConfig CreateRoute(VKProxyIngressContext ingressContext, V1HTTPIngressPath path, ClusterConfig cluster, string host)
    {
        var p = ConvertPath(path);
        return new RouteConfig()
        {
            Match = new RouteMatch()
            {
                Hosts = host is not null ? new[] { host } : AnyHosts,
                Paths = new[] { p },
                Methods = ingressContext.Options.RouteMethods,
                Statement = ingressContext.Options.RouteStatement,
            },
            ClusterId = cluster.Key,
            Key = $"{ingressContext.Ingress.Metadata.Name}.{ingressContext.Ingress.Metadata.NamespaceProperty}:{host}{path.Path}",
            Transforms = ingressContext.Options.Transforms,
            Timeout = ingressContext.Options.Timeout,
            Metadata = ingressContext.Options.RouteMetadata,
            Order = ingressContext.Options.RouteOrder,
            Limit = ingressContext.Options.Limit,
        };
    }

    private static string ConvertPath(V1HTTPIngressPath path)
    {
        var p = path.Path;
        if (string.Equals(path.PathType, "Prefix", StringComparison.OrdinalIgnoreCase))
        {
            return p + "*";
        }
        else
        {
            return p;
        }
    }

    private static ClusterConfig GetOrAddCluster(VKProxyIngressContext ingressContext, VKProxyConfigContext configContext, V1IngressServiceBackend ingressServiceBackend)
    {
        var key = UpstreamName(ingressContext.Ingress.Metadata.NamespaceProperty, ingressServiceBackend);
        var cluster = CollectionsMarshal.GetValueRefOrAddDefault(configContext.Clusters, key, out _) ??= new ClusterConfig();
        cluster.Key = key;
        var options = ingressContext.Options;
        cluster.LoadBalancingPolicy = options.LoadBalancingPolicy;
        cluster.HealthCheck = options.HealthCheck;
        cluster.HttpClientConfig = options.HttpClientConfig;
        cluster.HttpRequest = options.HttpRequest;
        cluster.Metadata = options.ClusterMetadata;
        if (cluster.Destinations == null)
        {
            cluster.Destinations = new List<DestinationConfig>();
        }
        return cluster;
    }

    private static string UpstreamName(string namespaceName, V1IngressServiceBackend ingressServiceBackend)
    {
        if (ingressServiceBackend is not null)
        {
            if (ingressServiceBackend.Port.Number.HasValue && ingressServiceBackend.Port.Number.Value > 0)
            {
                return $"{ingressServiceBackend.Name}.{namespaceName}:{ingressServiceBackend.Port.Number}";
            }

            if (!string.IsNullOrWhiteSpace(ingressServiceBackend.Port.Name))
            {
                return $"{ingressServiceBackend.Name}.{namespaceName}:{ingressServiceBackend.Port.Name}";
            }
        }

        return $"{namespaceName}-INVALID";
    }

    private static void HandleAnnotations(VKProxyIngressContext context, V1ObjectMeta metadata)
    {
        var annotations = metadata.Annotations;
        if (annotations is null)
        {
            return;
        }
        var options = context.Options;
        //cluster
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/load-balancing", out var loadBalancing))
        {
            options.LoadBalancingPolicy = loadBalancing;
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/health-check", out var healthCheck))
        {
            options.HealthCheck = YamlDeserializer.Deserialize<HealthCheckConfig>(healthCheck);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/http-client", out var httpClientConfig))
        {
            options.HttpClientConfig = YamlDeserializer.Deserialize<HttpClientConfig>(httpClientConfig);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/http-request", out var httpRequest))
        {
            options.HttpRequest = YamlDeserializer.Deserialize<ForwarderRequestConfig>(httpRequest);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/cluster-metadata", out var clusterMetadata))
        {
            options.ClusterMetadata = YamlDeserializer.Deserialize<Dictionary<string, string>>(clusterMetadata);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/backend-protocol", out var http))
        {
            options.Https = http.Equals("https", StringComparison.OrdinalIgnoreCase);
        }
        //route
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/transforms", out var transforms))
        {
            options.Transforms = YamlDeserializer.Deserialize<List<Dictionary<string, string>>>(transforms);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/route-order", out var routeOrder))
        {
            options.RouteOrder = int.Parse(routeOrder, CultureInfo.InvariantCulture);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/route-methods", out var routeMethods))
        {
            options.RouteMethods = YamlDeserializer.Deserialize<List<string>>(routeMethods).Distinct(StringComparer.OrdinalIgnoreCase).ToHashSet();
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/route-statement", out var statement))
        {
            HttpRoutingStatementParser.ConvertToFunction(statement);
            options.RouteStatement = statement;
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/route-metadata", out var routeMetadata))
        {
            options.RouteMetadata = YamlDeserializer.Deserialize<Dictionary<string, string>>(routeMetadata);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/timeout", out var timeout))
        {
            options.Timeout = TimeSpan.Parse(timeout, CultureInfo.InvariantCulture);
        }
        if (annotations.TryGetValue("vkproxy.ingress.kubernetes.io/limit", out var limit))
        {
            options.Limit = YamlDeserializer.Deserialize<ConcurrentConnectionLimitOptions>(limit);
        }
        //todo
    }

    private static bool MatchesPort(V1ServicePort port1, V1ServiceBackendPort port2)
    {
        if (port1 is null || port2 is null)
        {
            return false;
        }
        if (port2.Number is not null && port2.Number == port1.Port)
        {
            return true;
        }
        if (port2.Name is not null && string.Equals(port2.Name, port1.Name, StringComparison.Ordinal))
        {
            return true;
        }
        return false;
    }
}
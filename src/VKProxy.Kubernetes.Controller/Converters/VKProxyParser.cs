using k8s.Models;
using VKProxy.Kubernetes.Controller.Caching;
using YamlDotNet.Serialization;

namespace VKProxy.Kubernetes.Controller.Converters;

public static class VKProxyParser
{
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
    }

    private static void HandleAnnotations(VKProxyIngressContext context, V1ObjectMeta metadata)
    {
        var annotations = metadata.Annotations;
        if (annotations is null)
        {
            return;
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
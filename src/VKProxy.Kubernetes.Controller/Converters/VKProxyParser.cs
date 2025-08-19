using YamlDotNet.Serialization;

namespace VKProxy.Kubernetes.Controller.Converters;

internal static class VKProxyParser
{
    private const string ExternalNameServiceType = "ExternalName";
    private static readonly Deserializer YamlDeserializer = new();

    internal static void ConvertFromKubernetesIngress(VKProxyIngressContext ingressContext, VKProxyConfigContext configContext)
    {
        throw new NotImplementedException();
    }
}
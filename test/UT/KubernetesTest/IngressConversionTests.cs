using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VKProxy.HttpRoutingStatement;
using VKProxy.Kubernetes.Controller;
using VKProxy.Kubernetes.Controller.Caching;
using VKProxy.Kubernetes.Controller.Certificates;
using VKProxy.Kubernetes.Controller.Converters;

namespace UT.KubernetesTest;

public class IngressConversionTests
{
    [Theory]
    [InlineData("basic-ingress-ExternalName")]
    [InlineData("https")]
    [InlineData("https_EndpointSlice")]
    [InlineData("https-service-port-protocol")]
    [InlineData("ingress-class-not-set")]
    [InlineData("ingress-class-set")]
    [InlineData("ingress-class-set-not-vkproxy")]
    [InlineData("mapped-port")]
    [InlineData("missing-svc")]
    [InlineData("multiple-endpoints-ports")]
    [InlineData("multiple-endpoints-same-port")]
    [InlineData("multiple-hosts")]
    [InlineData("multiple-ingresses")]
    [InlineData("multiple-ingresses-one-svc")]
    [InlineData("multiple-namespaces")]
    [InlineData("port-diff-name")]
    [InlineData("port-mismatch")]
    [InlineData("route-methods")]
    [InlineData("route-order")]
    [InlineData("route-metadata")]
    [InlineData("route-statement")]
    [InlineData("cluster-annotations")]
    [InlineData("annotations")]
    public async Task ParsingTests(string name)
    {
        var ingressClass = KubeResourceGenerator.CreateIngressClass("vkproxy", "vkproxy/ingress", true);
        var cache = await GetKubernetesInfo(name, ingressClass);
        var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
        var configContext = new VKProxyConfigContext();
        var ingresses = cache.GetIngresses().ToArray();

        foreach (var ingress in ingresses)
        {
            if (cache.TryGetReconcileData(new NamespacedName(ingress.Metadata.NamespaceProperty, ingress.Metadata.Name), out var data))
            {
                var ingressContext = new VKProxyIngressContext(ingress, data.ServiceList, data.EndpointsList) { StatementFactory = new DefaultRouteStatementFactory() };
                VKProxyParser.ConvertFromKubernetesIngress(ingressContext, configContext);
            }
        }
        VerifyJson(System.Text.Json.JsonSerializer.Serialize(configContext.Build(), options), name, "ingress.json");
    }

    private static void VerifyJson(string json, string name, string fileName)
    {
        var other = File.ReadAllText(Path.Combine("testassets", name, fileName));
        json = StripNullProperties(json);
        other = StripNullProperties(other);

        var actual = JToken.Parse(json);
        var jOther = JToken.Parse(other);

        Assert.True(JToken.DeepEquals(actual, jOther), $"Expected: {jOther}\nActual: {actual}");
    }

    private static string StripNullProperties(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json));
        var sb = new StringBuilder();
        using var sw = new StringWriter(sb);
        using var writer = new JsonTextWriter(sw);
        while (reader.Read())
        {
            var token = reader.TokenType;
            var value = reader.Value;
            if (reader.TokenType == JsonToken.PropertyName)
            {
                reader.Read();
                if (reader.TokenType == JsonToken.Null)
                {
                    continue;
                }
                writer.WriteToken(token, value);
            }
            writer.WriteToken(reader.TokenType, reader.Value);
        }

        return sb.ToString();
    }

    private async Task<ICache> GetKubernetesInfo(string name, V1IngressClass ingressClass)
    {
        var mockLogger = new Mock<ILogger<IngressCache>>();
        var mockOptions = new Mock<IOptions<K8sOptions>>();
        var certificateSelector = new Mock<IServerCertificateSelector>();
        var loggerHelper = new Mock<ILogger<CertificateHelper>>();
        var certificateHelper = new CertificateHelper(loggerHelper.Object);

        mockOptions.SetupGet(o => o.Value).Returns(new K8sOptions { ControllerClass = "vkproxy/ingress" });

        var cache = new IngressCache(mockOptions.Object, certificateSelector.Object, certificateHelper, mockLogger.Object);

        var typeMap = new Dictionary<string, Type>();
        typeMap.Add("networking.k8s.io/v1/Ingress", typeof(V1Ingress));
        typeMap.Add("v1/Service", typeof(V1Service));
        typeMap.Add("v1/Endpoints", typeof(V1Endpoints));
        typeMap.Add("discovery.k8s.io/v1/EndpointSlice", typeof(V1EndpointSlice));
        typeMap.Add("v1/Secret", typeof(V1Secret));

        if (ingressClass is not null)
        {
            cache.Update(WatchEventType.Added, ingressClass);
        }

        var kubeObjects = await KubernetesYaml.LoadAllFromFileAsync(Path.Combine("testassets", name, "ingress.yaml"), typeMap).ConfigureAwait(false);

        foreach (var obj in kubeObjects)
        {
            if (obj is V1Ingress ingress)
            {
                cache.Update(WatchEventType.Added, ingress);
            }
            else if (obj is V1Service service)
            {
                cache.Update(WatchEventType.Added, service);
            }
            else if (obj is V1Endpoints endpoints)
            {
                cache.Update(WatchEventType.Added, endpoints);
            }
            else if (obj is V1EndpointSlice endpointSlices)
            {
                cache.Update(WatchEventType.Added, endpointSlices);
            }
            else if (obj is V1Secret secret)
            {
                cache.Update(WatchEventType.Added, secret);
            }
        }

        return cache;
    }
}
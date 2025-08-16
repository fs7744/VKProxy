namespace VKProxy.Kubernetes.Controller;

public class K8sOptions
{
    /// <summary>
    /// Defines a name of the ingress controller. IngressClass ".spec.controller" field should match this.
    /// This field is required.
    /// </summary>
    public string ControllerClass { get; set; } = "vkproxy/ingress";

    public bool ServerCertificates { get; set; }

    public string DefaultSslCertificate { get; set; }

    /// <summary>
    /// Name of the Kubernetes Service the ingress controller is running in.
    /// This field is required.
    /// </summary>
    public string ControllerServiceName { get; set; } = "vkproxy-controller";

    /// <summary>
    /// Namespace of the Kubernetes Service the ingress controller is running in.
    /// This field is required.
    /// </summary>
    public string ControllerServiceNamespace { get; set; } = "vkproxy";
}
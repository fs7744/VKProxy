using k8s.Models;
using System.Security.Cryptography.X509Certificates;

namespace VKProxy.Kubernetes.Controller.Certificates;

public interface ICertificateHelper
{
    X509Certificate2 ConvertCertificate(NamespacedName namespacedName, V1Secret secret);
}
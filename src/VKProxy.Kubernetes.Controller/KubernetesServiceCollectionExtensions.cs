using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VKProxy.Kubernetes.Controller.Caching;
using VKProxy.Kubernetes.Controller.Certificates;
using VKProxy.Kubernetes.Controller.Client;
using VKProxy.Kubernetes.Controller.Services;

namespace VKProxy.Kubernetes.Controller;

public static class KubernetesServiceCollectionExtensions
{
    public static IServiceCollection AddKubernetesCore(this IServiceCollection services)
    {
        if (!services.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(IKubernetes)))
        {
            services = services.Configure<KubernetesClientOptions>(options =>
            {
                options.Configuration ??= KubernetesClientConfiguration.BuildDefaultConfig();
            });

            services = services.AddSingleton<IKubernetes>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<KubernetesClientOptions>>().Value;

                return new k8s.Kubernetes(options.Configuration);
            });
        }

        return services;
    }

    public static IServiceCollection AddKubernetesIngressMonitor(this IServiceCollection services, Action<K8sOptions> config = null)
    {
        services.Configure<K8sOptions>(i =>
        {
            config?.Invoke(i);
        });

        services.AddHostedService<IngressController>();
        services.AddSingleton<ICache, IngressCache>();
        services.AddTransient<IReconciler, Reconciler>();

        services.AddSingleton<IServerCertificateSelector, ServerCertificateSelector>();
        services.AddSingleton<ICertificateHelper, CertificateHelper>();
        services.AddKubernetesCore();

        return services;
    }

    public static IMvcBuilder AddKubernetesDispatchController(this IMvcBuilder builder)
    {
        //builder.AddApplicationPart(typeof(DispatchController).Assembly);
        return builder;
    }
}
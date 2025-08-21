using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using VKProxy.HttpRoutingStatement;
using VKProxy.Kubernetes.Controller.Caching;
using VKProxy.Kubernetes.Controller.Certificates;
using VKProxy.Kubernetes.Controller.Client;
using VKProxy.Kubernetes.Controller.Hosting;
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
        services.AddTransient<ICache, IngressCache>();
        services.AddTransient<IReconciler, Reconciler>();

        services.RegisterResourceInformer<V1Ingress, V1IngressResourceInformer>();
        services.RegisterResourceInformer<V1Service, V1ServiceResourceInformer>();
        services.RegisterResourceInformer<V1Endpoints, V1EndpointsResourceInformer>();
        services.RegisterResourceInformer<V1EndpointSlice, V1EndpointSlicesResourceInformer>();
        services.RegisterResourceInformer<V1IngressClass, V1IngressClassResourceInformer>();
        services.RegisterResourceInformer<V1Secret, V1SecretResourceInformer>("type=kubernetes.io/tls");
        services.TryAddSingleton<IRouteStatementFactory, DefaultRouteStatementFactory>();

        services.AddSingleton<IServerCertificateSelector, ServerCertificateSelector>();
        services.AddSingleton<ICertificateHelper, CertificateHelper>();
        services.AddSingleton<IIngressResourceStatusUpdater, V1IngressResourceStatusUpdater>();
        services.AddKubernetesCore();

        return services;
    }

    public static IMvcBuilder AddKubernetesDispatchController(this IMvcBuilder builder)
    {
        //builder.AddApplicationPart(typeof(DispatchController).Assembly);
        return builder;
    }

    public static IServiceCollection RegisterResourceInformer<TResource, TService>(this IServiceCollection services)
        where TResource : class, IKubernetesObject<V1ObjectMeta>, new()
        where TService : IResourceInformer<TResource>
    {
        return services.RegisterResourceInformer<TResource, TService>(null);
    }

    public static IServiceCollection RegisterResourceInformer<TResource, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services, string fieldSelector)
        where TResource : class, IKubernetesObject<V1ObjectMeta>, new()
        where TService : IResourceInformer<TResource>
    {
        services.AddSingleton(new ResourceSelector<TResource>(fieldSelector));
        services.AddSingleton(typeof(IResourceInformer<TResource>), typeof(TService));

        return services.RegisterHostedService<IResourceInformer<TResource>>();
    }

    public static IServiceCollection RegisterHostedService<TService>(this IServiceCollection services)
        where TService : IHostedService
    {
        if (!services.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(HostedServiceAdapter<TService>)))
        {
            services = services.AddHostedService<HostedServiceAdapter<TService>>();
        }

        return services;
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VKProxy.Kubernetes.Controller;
using VKProxy.Storages.Etcd;

namespace VKProxy.CommandLine.Kubernetes;

internal class K8SCommand : ArgsCommand<K8sOptions>
{
    public K8SCommand() : base("k8s", "K8S controller for VKProxy")
    {
        AddArg(new CommandArg("controller-class", null, null, "Defines a name of the ingress controller. IngressClass \".spec.controller\" field should match this.", s =>
        {
            if (!string.IsNullOrWhiteSpace(s))
                Args.ControllerClass = s;
        }));

        AddArg(new CommandArg("controller-service-name", null, null, "Name of the Kubernetes Service the ingress controller is running in.", s =>
        {
            if (!string.IsNullOrWhiteSpace(s))
                Args.ControllerServiceName = s;
        }));

        AddArg(new CommandArg("controller-service-namespace", null, null, "Namespace of the Kubernetes Service the ingress controller is running in.", s =>
        {
            if (!string.IsNullOrWhiteSpace(s))
                Args.ControllerServiceNamespace = s;
        }));

        AddArg(new CommandArg("default-ssl-certificate", null, null, "Default Ssl Certificate", s =>
        {
            if (!string.IsNullOrWhiteSpace(s))
                Args.DefaultSslCertificate = s;
        }));

        AddArg(new CommandArg("server-certificates", null, null, "Server Certificates", s =>
        {
            if (bool.TryParse(s, out var b))
                Args.ServerCertificates = b;
        }));
    }

    protected override Task ExecAsync()
    {
        var b = WebApplication.CreateBuilder();
        b.Services.AddKubernetesIngressMonitor(op =>
        {
            op.ControllerClass = Args.ControllerClass;
            op.ControllerServiceName = Args.ControllerServiceName;
            op.ControllerServiceNamespace = Args.ControllerServiceNamespace;
            op.DefaultSslCertificate = Args.DefaultSslCertificate;
            op.ServerCertificates = Args.ServerCertificates;
        });

        b.Services.UseEtcdConfigFromEnv();
        b.Services.AddControllers()
            .AddKubernetesDispatchController();

        var app = b.Build();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        return app.RunAsync();
    }
}
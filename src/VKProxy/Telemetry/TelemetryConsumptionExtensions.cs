using Microsoft.Extensions.DependencyInjection;

namespace VKProxy.Telemetry;

public static class TelemetryConsumptionExtensions
{    /// <summary>
     /// Registers all telemetry listeners (Forwarder, Kestrel, Http, NameResolution, NetSecurity, Sockets and WebSockets).
     /// </summary>
    public static IServiceCollection AddTelemetryListeners(this IServiceCollection services)
    {
        services.AddHostedService<KestrelEventListenerService>();
        //services.AddHostedService<WebSocketsEventListenerService>();
        //services.AddHostedService<ForwarderEventListenerService>();
        //services.AddHostedService<HttpEventListenerService>();
        //services.AddHostedService<NameResolutionEventListenerService>();
        //services.AddHostedService<NetSecurityEventListenerService>();
        //services.AddHostedService<SocketsEventListenerService>();
        return services;
    }
}
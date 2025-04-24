using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Reflection;
using VKProxy.Config;
using VKProxy.Core.Loggers;

namespace VKProxy.Health.ActiveHealthCheckers;

public class HttpActiveHealthChecker : ActiveHealthCheckerBase
{
    private static readonly string? version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    private static readonly string defaultUserAgent = $"VKProxy{(string.IsNullOrEmpty(version) ? "" : $"/{version.Split('+')[0]}")} (Active Health Check Monitor)";
    public override string Name => "Http";

    public HttpActiveHealthChecker(ProxyLogger logger) : base(logger)
    {
    }

    protected override async ValueTask<bool> DoCheckAsync(ActiveHealthCheckConfig config, DestinationState state, CancellationToken cancellationToken)
    {
        var probeAddress = state.Address;
        var probePath = config.Path;
        UriHelper.FromAbsolute(probeAddress, out var destinationScheme, out var destinationHost, out var destinationPathBase, out _, out _);
        var query = QueryString.FromUriComponent(config.Query ?? "");
        var probeUri = UriHelper.BuildAbsolute(destinationScheme, destinationHost, destinationPathBase, probePath, query);
        HttpMethod method;
        if (string.IsNullOrWhiteSpace(config.Method))
        {
            method = HttpMethod.Get;
        }
        else
        {
            method = HttpMethod.Parse(config.Method);
        }
        var request = new HttpRequestMessage(method, probeUri)
        {
            Version = state.ClusterConfig.HttpRequest?.Version ?? HttpVersion.Version20,
            VersionPolicy = state.ClusterConfig.HttpRequest?.VersionPolicy ?? HttpVersionPolicy.RequestVersionOrLower,
        };
        if (!string.IsNullOrEmpty(state.Host))
        {
            request.Headers.Add(HeaderNames.Host, state.Host);
        }

        request.Headers.Add(HeaderNames.UserAgent, defaultUserAgent);

        var resp = await state.ClusterConfig.HttpMessageHandler.SendAsync(request, cancellationToken);
        return resp.IsSuccessStatusCode;
    }
}
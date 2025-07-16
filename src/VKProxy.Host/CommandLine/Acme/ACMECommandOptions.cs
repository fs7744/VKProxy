using Microsoft.Extensions.DependencyInjection;
using VKProxy.ACME;

namespace VKProxy.CommandLine;

public class ACMECommandOptions
{
    public Uri Server { get; set; } = WellKnownServers.LetsEncryptStagingV2;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool? DangerousAcceptAnyServerCertificate { get; set; } = false;
    public Uri? WebProxy { get; set; }

    public static void AddCommonArgs<T>(ArgsCommand<T> command) where T : ACMECommandOptions, new()
    {
        command.AddArg(new CommandArg("server", "s", null, $"The dictionary URI to an ACME server. (default is test server: {WellKnownServers.LetsEncryptStagingV2})", s => command.Args.Server = new Uri(s)));
        command.AddArg(new CommandArg("timeout", null, null, $"Timeout of http request. (default is 00:00:30)", s => command.Args.Timeout = TimeSpan.Parse(s)));
        command.AddArg(new CommandArg("web-proxy", null, null, "The URI of the proxy server.", s => command.Args.WebProxy = new Uri(s)));
        command.AddArg(new CommandArg("dangerous-certificate", null, null, "Dangerous accept any server certificate.", s => command.Args.DangerousAcceptAnyServerCertificate = bool.Parse(s)));
    }

    public async Task<IAcmeContext> GetAcmeContextAsync(CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddACME(c =>
        {
            c.HttpClientConfig = new Config.HttpClientConfig()
            {
                DangerousAcceptAnyServerCertificate = this.DangerousAcceptAnyServerCertificate
            };
            if (WebProxy != null)
            {
                c.HttpClientConfig.WebProxy = new Config.WebProxyConfig() { Address = WebProxy, BypassOnLocal = true };
            }
        });
        var context = services.BuildServiceProvider().GetRequiredService<IAcmeContext>();
        await context.InitAsync(Server, cancellationToken);
        return context;
    }

    public CancellationTokenSource GetCancellationTokenSource() => new CancellationTokenSource(Timeout);
}
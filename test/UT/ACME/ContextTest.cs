using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VKProxy;
using VKProxy.ACME;

namespace UT.ACME;

public class ContextTest
{
    [Fact]
    public async Task CmdTest()
    {
        await VKProxyHost.RunAsync("acme account update --debug".Split(' '));
    }

    //[Fact]
    //public async Task DoTest()
    //{
    //    var services = new ServiceCollection();
    //    services.AddACME();
    //    var p = services.BuildServiceProvider();
    //    var client = p.GetRequiredService<IAcmeClient>();
    //    var context = p.GetRequiredService<IAcmeContext>();
    //    await context.InitAsync(WellKnownServers.LetsEncryptStagingV2, CancellationToken.None);
    //    //var nonce = await client.NewNonceAsync(context.Directory.NewNonce, CancellationToken.None);
    //}

    //[Fact]
    //public async Task AccountKeyTest()
    //{
    //    await VKProxyHost.RunAsync("acme account update -k D:\\code\\github\\VKProxy\\src\\VKProxy.Cli\\bin\\Debug\\net9.0\\key -c mailto:xxx@xxx.com,mailto:test@xxx.com --debug".Split(' '));
    //}
}
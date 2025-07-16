using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VKProxy.ACME;

namespace UT.ACME;

public class ContextTest
{
    [Fact]
    public async Task DoTest()
    {
        var services = new ServiceCollection();
        services.AddACME();
        var p = services.BuildServiceProvider();
        var client = p.GetRequiredService<IAcmeClient>();
        var context = p.GetRequiredService<IAcmeContext>();
        await context.InitAsync(WellKnownServers.LetsEncryptStagingV2, CancellationToken.None);
        //context.NewAccountAsync();
        //var nonce = await client.NewNonceAsync(context.Directory.NewNonce, CancellationToken.None);
    }

    [Fact]
    public async Task AccountKeyTest()
    {
    }
}
using Microsoft.Extensions.DependencyInjection;
using VKProxy;
using VKProxy.ACME;
using VKProxy.ACME.Resource;

namespace UT.ACME;

public class ContextTest
{
    [Fact]
    public async Task CmdTest()
    {
        await VKProxyHost.RunAsync("acme account update --debug".Split(' '));
    }

    //[Fact]
    //public async Task Test()
    //{
    //    await DoTest();
    //}

    public async Task DoTest()
    {
        var testKey = """
            -----BEGIN RSA PRIVATE KEY-----
            MIIEowIBAAKCAQEAk25QJ2JnO2ZZSXe50aLgxMIihRIL021D8NzIyVS/teF2ocBF
            KnsA29sllafzRsfIDlifO55zZHr94HVQ98qWWhYHkcxXqccNd+5k+s+QT2n5Rdma
            Oa31MCNY1lqP8XeqrbGPokI9g03G1bXuelxD+SPi5V+D++2kjomAoS9YUcwAO8Nq
            Eo+rQ6nAdaLLEzkbwgIdt+cuy3SqpZxRAzCp6AzDv7GkkJtALDoROKbeIa+f18j7
            oITV/CENPTTw5KAmXV7CdPZkSIj/WO4sQRsNNCHHMoD8vYYs9wCrh1KPxrlh+77S
            M1GcvNxsiNLf3L5vjUbqXsjZog/xB5gqCNMaXQIDAQABAoIBADuq/c2yyc0Ek0Zk
            qlPp88YuPAJXV2nuYvzsnma9YgmegoDcbCHRPnu8qe1z18XhvVnxDCD49ALKtE1P
            rcFbwJYdLFsZtLEF2rGbTkskDmfVoAlhFEYb3YvqAl5esLstj2pU3qjw3ixyIfME
            eswS43/FwmLK0YP4ng0CIYkavEf+Bxv89CLTPB+cit0Fl8mhKychCzSDK534lV1i
            dSqpwoL9GOyV0lKpGVunztf7Tqde+wEV3IGWMT0R7x0qW8A/hVLQuUqIwmbh7oRm
            9P/jC3VHU9JQoWruskhcbAlAjXC/57k+xNPKzmIcm6DUP/ccNdrJW66uG567BUiL
            PU4scBECgYEAx0X3svzW51K4H6A9w8DU7U37AWL/vPxcTK9iXi0ZF5sNKZV1Kxhg
            IBiFm80uok1rdWdkVA9MtEpwdvHMtMQxvAApsVaM/Qj/S8QwkI/0zp5GvUHyiUWm
            1DGAOGCY2GoqYN3l8SbT6TB37Vmhx3CQ6taQjy8kzWziePPzFqiUDhsCgYEAvWZV
            TsWoaAe3HY5kjrj1rpXiQetelUcBU0mUYQwTCw4I8iIQUbNEFPN1CugIP9GCaksr
            Ccmi0BZEkG9fz07rrbXAPhBgtTLBg1bDTvQN2fk9lzlJbNUHaJv5PvBmfj/6DkUz
            qWRc8SfVDoQ+QKghz7KDBjZ09s/Fdn+Bp9fVIOcCgYA4plxnhtd1RZ/QZdaJOt2N
            ZNjRqRo42KlIp6dYTIvQmSShyLpZeQGCvlXlV+xE+att4em0t/C/ZFYailz+mrPk
            1UsE/izwlkk1ed2wiyw2POqxTPktKx7lPflMjbGF/JB1nz+KUdZ2eW/uiseiEg8w
            o7TO78EPoT+00O0vaNdGNwKBgBeXPnrwTbifdWR+DvJkAV38l1EEoyRO0tBv8sZf
            vaN73QtjyMqUXJ+Lb4GrQxPH4cmhkTvH3Lq0e1fON43X060wXUCdw53uM4JLdUpJ
            Rcxnqg9C+G1Q33pdKx92zB1flKLgZb3snVMAVh5XxHVDO+rl3kIQ2GLBoGPRH/Ir
            BQXzAoGBAJw5v8U975VU6zliTZdlk89YxYZq0RcrbEvrKDZPkrqQUTzrqxDhlPKB
            tMdZvPgxDYFoJbYkgRnlWVMKZYh14SxpzShcDJqLJt4pNv46qQa0PB7FFj685E6P
            gQAKCnDh+93SBc/4nKlYO3FWczQLWujyGdHqouchdgyZ5TtXu0RY
            -----END RSA PRIVATE KEY-----

            """;
        var services = new ServiceCollection();
        services.AddACME(c =>
        {
            c.HttpClientConfig = new VKProxy.Config.HttpClientConfig()
            {
                DangerousAcceptAnyServerCertificate = true
            };
        });
        var p = services.BuildServiceProvider();
        var client = p.GetRequiredService<IAcmeClient>();
        var context = p.GetRequiredService<IAcmeContext>();
        await context.InitAsync(new Uri("https://127.0.0.1:14000/dir"), CancellationToken.None);
        await NewAccounts(context);
        await context.NewAccountAsync(new string[] { "mailto:xxx@xxx.com" }, true, testKey);
        await NewOrders(context);
        Assert.Empty(context.ListOrdersAsync().ToBlockingEnumerable().ToArray());
        await HttpChallenge(context);

        //await context.AccountAsync(testKey);
        //var order = await context.NewOrderAsync(new string[] { "test.com", "*.test.com" });
        //var order = context.ListOrdersAsync().ToBlockingEnumerable().First();
        //var aus = order.GetAuthorizationsAsync().ToBlockingEnumerable().ToArray();

        //var ausi = new List<Authorization>();
        //foreach (var auth in aus)
        //{
        //    ausi.Add(await auth.GetResourceAsync());
        //}
        //var d = await order.GetResourceAsync();
        //var os = context.ListOrdersAsync().ToBlockingEnumerable().ToArray();
        //var nonce = await client.NewNonceAsync(context.Directory.NewNonce, CancellationToken.None);
    }

    private async Task HttpChallenge(IAcmeContext context)
    {
        var order = await context.NewOrderAsync(new string[] { "test.com" });
        var aus = order.GetAuthorizationsAsync().ToBlockingEnumerable().ToArray();
        Assert.NotEmpty(aus);
        var a = aus.First();
        var b = await a.HttpAsync();
        var c = await b.ValidateAsync();
        var r = await order.DownloadAsync();
    }

    private async Task NewAccounts(IAcmeContext context)
    {
        var account = await context.NewAccountAsync(new string[] { "mailto:xxx@xxx.com" }, true, KeyAlgorithm.RS256.NewKey());
        var a = await account.GetResourceAsync();
        Assert.Equal(AccountStatus.Valid, a.Status);
        await account.ChangeKeyAsync(KeyAlgorithm.ES384.NewKey());
        await account.UpdateAsync(new string[] { "mailto:xxx@xt.com" });
        a = await account.GetResourceAsync();
        Assert.Equal(AccountStatus.Valid, a.Status);
        await account.DeactivateAsync();
        await Assert.ThrowsAsync<AcmeException>(() => account.GetResourceAsync());
    }

    private async Task NewOrders(IAcmeContext context)
    {
        await context.NewOrderAsync(new string[] { "test.com" });
        var orders = context.ListOrdersAsync().ToBlockingEnumerable().ToArray();
        Assert.True(orders.Any());
        foreach (var o in orders)
        {
            var oo = await o.GetResourceAsync();
            var aus = o.GetAuthorizationsAsync().ToBlockingEnumerable().ToArray();
            foreach (var item in aus)
            {
                var c = await item.GetResourceAsync();
                Assert.Equal(AuthorizationStatus.Pending, c.Status);
                var dd = item.GetChallengesAsync().ToBlockingEnumerable().ToArray();
                foreach (var challenge in dd)
                {
                    var cc = await challenge.GetResourceAsync();
                    Assert.Equal(ChallengeStatus.Pending, cc.Status);
                }
                await item.DeactivateAsync();
            }
        }
    }

    //[Fact]
    //public async Task AccountKeyTest()
    //{
    //    await VKProxyHost.RunAsync("acme account update -k D:\\code\\github\\VKProxy\\src\\VKProxy.Cli\\bin\\Debug\\net9.0\\key -c mailto:xxx@xxx.com,mailto:test@xxx.com --debug".Split(' '));
    //}
}
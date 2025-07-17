using Google.Rpc.Context;
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
    //    var testKey = """
    //        -----BEGIN RSA PRIVATE KEY-----
    //        MIIEowIBAAKCAQEAk25QJ2JnO2ZZSXe50aLgxMIihRIL021D8NzIyVS/teF2ocBF
    //        KnsA29sllafzRsfIDlifO55zZHr94HVQ98qWWhYHkcxXqccNd+5k+s+QT2n5Rdma
    //        Oa31MCNY1lqP8XeqrbGPokI9g03G1bXuelxD+SPi5V+D++2kjomAoS9YUcwAO8Nq
    //        Eo+rQ6nAdaLLEzkbwgIdt+cuy3SqpZxRAzCp6AzDv7GkkJtALDoROKbeIa+f18j7
    //        oITV/CENPTTw5KAmXV7CdPZkSIj/WO4sQRsNNCHHMoD8vYYs9wCrh1KPxrlh+77S
    //        M1GcvNxsiNLf3L5vjUbqXsjZog/xB5gqCNMaXQIDAQABAoIBADuq/c2yyc0Ek0Zk
    //        qlPp88YuPAJXV2nuYvzsnma9YgmegoDcbCHRPnu8qe1z18XhvVnxDCD49ALKtE1P
    //        rcFbwJYdLFsZtLEF2rGbTkskDmfVoAlhFEYb3YvqAl5esLstj2pU3qjw3ixyIfME
    //        eswS43/FwmLK0YP4ng0CIYkavEf+Bxv89CLTPB+cit0Fl8mhKychCzSDK534lV1i
    //        dSqpwoL9GOyV0lKpGVunztf7Tqde+wEV3IGWMT0R7x0qW8A/hVLQuUqIwmbh7oRm
    //        9P/jC3VHU9JQoWruskhcbAlAjXC/57k+xNPKzmIcm6DUP/ccNdrJW66uG567BUiL
    //        PU4scBECgYEAx0X3svzW51K4H6A9w8DU7U37AWL/vPxcTK9iXi0ZF5sNKZV1Kxhg
    //        IBiFm80uok1rdWdkVA9MtEpwdvHMtMQxvAApsVaM/Qj/S8QwkI/0zp5GvUHyiUWm
    //        1DGAOGCY2GoqYN3l8SbT6TB37Vmhx3CQ6taQjy8kzWziePPzFqiUDhsCgYEAvWZV
    //        TsWoaAe3HY5kjrj1rpXiQetelUcBU0mUYQwTCw4I8iIQUbNEFPN1CugIP9GCaksr
    //        Ccmi0BZEkG9fz07rrbXAPhBgtTLBg1bDTvQN2fk9lzlJbNUHaJv5PvBmfj/6DkUz
    //        qWRc8SfVDoQ+QKghz7KDBjZ09s/Fdn+Bp9fVIOcCgYA4plxnhtd1RZ/QZdaJOt2N
    //        ZNjRqRo42KlIp6dYTIvQmSShyLpZeQGCvlXlV+xE+att4em0t/C/ZFYailz+mrPk
    //        1UsE/izwlkk1ed2wiyw2POqxTPktKx7lPflMjbGF/JB1nz+KUdZ2eW/uiseiEg8w
    //        o7TO78EPoT+00O0vaNdGNwKBgBeXPnrwTbifdWR+DvJkAV38l1EEoyRO0tBv8sZf
    //        vaN73QtjyMqUXJ+Lb4GrQxPH4cmhkTvH3Lq0e1fON43X060wXUCdw53uM4JLdUpJ
    //        Rcxnqg9C+G1Q33pdKx92zB1flKLgZb3snVMAVh5XxHVDO+rl3kIQ2GLBoGPRH/Ir
    //        BQXzAoGBAJw5v8U975VU6zliTZdlk89YxYZq0RcrbEvrKDZPkrqQUTzrqxDhlPKB
    //        tMdZvPgxDYFoJbYkgRnlWVMKZYh14SxpzShcDJqLJt4pNv46qQa0PB7FFj685E6P
    //        gQAKCnDh+93SBc/4nKlYO3FWczQLWujyGdHqouchdgyZ5TtXu0RY
    //        -----END RSA PRIVATE KEY-----

    //        """;
    //    var services = new ServiceCollection();
    //    services.AddACME(c =>
    //    {
    //        c.HttpClientConfig = new VKProxy.Config.HttpClientConfig()
    //        {
    //            DangerousAcceptAnyServerCertificate = true
    //        };
    //    });
    //    var p = services.BuildServiceProvider();
    //    var client = p.GetRequiredService<IAcmeClient>();
    //    var context = p.GetRequiredService<IAcmeContext>();
    //    await context.InitAsync(new Uri("https://127.0.0.1:14000/dir"), CancellationToken.None);
    //    //await context.InitAsync(WellKnownServers.LetsEncryptStagingV2, CancellationToken.None);
    //    await context.NewAccountAsync(new string[] { "mailto:xxx@xxx.com" }, true, testKey);
    //    //await context.AccountAsync(testKey);
    //    var order = await context.NewOrderAsync(new string[] { "test.com", "*.test.com" });
    //    var d = await order.GetResourceAsync();
    //    //var os = context.ListOrdersAsync().ToBlockingEnumerable().ToArray();
    //    //var nonce = await client.NewNonceAsync(context.Directory.NewNonce, CancellationToken.None);
    //}

    //[Fact]
    //public async Task AccountKeyTest()
    //{
    //    await VKProxyHost.RunAsync("acme account update -k D:\\code\\github\\VKProxy\\src\\VKProxy.Cli\\bin\\Debug\\net9.0\\key -c mailto:xxx@xxx.com,mailto:test@xxx.com --debug".Split(' '));
    //}
}
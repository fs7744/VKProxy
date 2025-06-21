using Microsoft.Extensions.Hosting;

namespace VKProxy.Cli;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = VKProxyHost.CreateBuilder(args)?.Build();
        if (app != null)
            await app.RunAsync();
    }
}
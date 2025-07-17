using System.Text.Json;
using VKProxy.ACME;

namespace VKProxy.CommandLine;

internal class ListOrderCommand : ArgsCommand<AccountCommandOptions>
{
    public ListOrderCommand() : base("list", "List all orders of account.")
    {
        AccountCommandOptions.AddCommonArgs(this);
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        await conetxt.AccountAsync(Args.AccountKey, cancellationToken: token);
        await foreach (var u in conetxt.ListOrdersAsync(token))
        {
            var d = await u.GetResourceAsync(token);
            Console.WriteLine(u.Location);
            Console.WriteLine(JsonSerializer.Serialize(d, DefaultAcmeHttpClient.JsonSerializerOptions));
            Console.WriteLine();
        }
    }
}
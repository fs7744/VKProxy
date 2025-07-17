using System.Text.Json;
using VKProxy.ACME;

namespace VKProxy.CommandLine;

internal class CheckAccountCommand : ArgsCommand<NewAccountCommandOptions>
{
    public CheckAccountCommand() : base("check", "Check account.")
    {
        AccountCommandOptions.AddCommonArgs(this);
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        var account = await conetxt.AccountAsync(Args.AccountKey, cancellationToken: token);
        var a = await account.GetResourceAsync(token);
        Console.WriteLine($"Location: {account.Location}");
        Console.Write("Account: ");
        Console.WriteLine(JsonSerializer.Serialize(a, DefaultAcmeHttpClient.JsonSerializerOptions));
    }
}
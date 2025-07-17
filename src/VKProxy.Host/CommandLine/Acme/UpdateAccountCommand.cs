using System.Text.Json;
using VKProxy.ACME;

namespace VKProxy.CommandLine;

internal class UpdateAccountCommand : ArgsCommand<NewAccountCommandOptions>
{
    public UpdateAccountCommand() : base("update", "Update contact for ACME account.")
    {
        AccountCommandOptions.AddCommonArgs(this);
        AddArg(new CommandArg("contact", "c", null, $"The contact for ACME account (E-mail should: mailto:xx@example.org,mailto:bb@example.org).", s =>
        {
            Args.Contact = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }));
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        var account = await conetxt.AccountAsync(Args.AccountKey, cancellationToken: token);
        var a = await account.UpdateAsync(Args.Contact, token);
        Console.WriteLine($"Location: {account.Location}");
        Console.Write("Account: ");
        Console.WriteLine(JsonSerializer.Serialize(a, DefaultAcmeHttpClient.JsonSerializerOptions));
    }
}

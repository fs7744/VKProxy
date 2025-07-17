using System.Text.Json;
using VKProxy.ACME;

namespace VKProxy.CommandLine;

internal class NewAccountCommand : ArgsCommand<NewAccountCommandOptions>
{
    public NewAccountCommand() : base("new", "Create new ACME account.")
    {
        AccountCommandOptions.AddCommonArgs(this);
        AddArg(new CommandArg("contact", "c", null, $"The contact for ACME account (E-mail should: mailto:xx@example.org,mailto:bb@example.org).", s =>
        {
            Args.Contact = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }));
        AddArg(new CommandArg("terms-of-service-agreed", null, null, $"terms of service agreed.", s =>
        {
            Args.TermsOfServiceAgreed = bool.Parse(s);
        }));
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        var account = await conetxt.NewAccountAsync(Args.Contact, Args.TermsOfServiceAgreed, Args.AccountKey, cancellationToken: token);
        var a = await account.GetResourceAsync(token);
        Console.WriteLine($"Location: {account.Location}");
        Console.Write("Account: ");
        Console.WriteLine(JsonSerializer.Serialize(a, DefaultAcmeHttpClient.JsonSerializerOptions));
    }
}

public class NewAccountCommandOptions : AccountCommandOptions
{
    public IList<string> Contact { get; set; }
    public bool TermsOfServiceAgreed { get; set; } = true;
}
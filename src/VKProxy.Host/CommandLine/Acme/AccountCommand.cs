using VKProxy.ACME.Crypto;

namespace VKProxy.CommandLine;

internal class AccountCommand : CommandGroup
{
    public AccountCommand() : base("account", "Manage ACME accounts.")
    {
        Add(new NewAccountKeyCommand());
        Add(new NewAccountCommand());
    }
}

internal class NewAccountCommand : ArgsCommand<NewAccountCommandOptions>
{
    public NewAccountCommand() : base("new", "Create new ACME account.")
    {
        ACMECommandOptions.AddCommonArgs(this);
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var conetxt = await Args.GetAcmeContextAsync(s.Token);
        await conetxt.NewAccountAsync(Args.Contact, Args.TermsOfServiceAgreed, Args.AccountKey, cancellationToken: s.Token);
    }
}

public class AccountCommandOptions : ACMECommandOptions
{
    public IKey AccountKey { get; set; }
}

public class NewAccountCommandOptions : AccountCommandOptions
{
    public IList<string> Contact { get; set; }
    public bool TermsOfServiceAgreed { get; set; } = true;
}
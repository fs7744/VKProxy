namespace VKProxy.CommandLine;

internal class TermsCommand : ArgsCommand<ACMECommandOptions>
{
    public TermsCommand() : base("terms", "view current terms of service.")
    {
        ACMECommandOptions.AddCommonArgs(this);
    }

    public override Func<Task> Do()
    {
        return GetTermsAsync;
    }

    private async Task GetTermsAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var conetxt = await Args.GetAcmeContextAsync(s.Token);
        Console.WriteLine(conetxt.Directory.Meta.TermsOfService);
    }
}

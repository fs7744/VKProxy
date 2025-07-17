using System.Text.Json;
using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace VKProxy.CommandLine;

internal class ChangeAccountKeyCommand : ArgsCommand<ChangeAccountKeyCommandOptions>
{
    public ChangeAccountKeyCommand() : base("change-key", "Change account key.")
    {
        ChangeAccountKeyCommandOptions.AddCommonArgs(this);
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        var account = await conetxt.AccountAsync(Args.AccountKey, cancellationToken: token);
        var a = await account.ChangeKeyAsync(Args.NewAccountKey, token);
        Console.WriteLine($"Location: {account.Location}");
        Console.Write("Account: ");
        Console.WriteLine(JsonSerializer.Serialize(a, DefaultAcmeHttpClient.JsonSerializerOptions));
    }
}

public class ChangeAccountKeyCommandOptions : AccountCommandOptions
{
    public Key NewAccountKey { get; set; }

    public static void AddCommonArgs<T>(ArgsCommand<T> command) where T : ChangeAccountKeyCommandOptions, new()
    {
        AccountCommandOptions.AddCommonArgs(command);
        command.AddArg(new CommandArg("new-key", null, null, $"Account new key path", s =>
        {
            var ss = File.ReadAllText(s);
            try
            {
                command.Args.NewAccountKey = KeyAlgorithmProvider.GetKey(ss);
            }
            catch (Exception)
            {
                var bytes = File.ReadAllBytes(s);
                command.Args.NewAccountKey = KeyAlgorithmProvider.GetKey(bytes);
            }
        }, check: () => command.Args.NewAccountKey != null));
    }
}
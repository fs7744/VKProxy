using DotNext;
using System.Text.Json;
using VKProxy.ACME;

namespace VKProxy.CommandLine;

internal class NewOrderCommand : ArgsCommand<NewOrderCommandOptions>
{
    public NewOrderCommand() : base("new", "new order.")
    {
        AccountCommandOptions.AddCommonArgs(this);
        AddArg(new CommandArg("domains", "d", null, $"The domains for the certificate order (like: test.example.org,www.example.org).", s =>
        {
            Args.Domains = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }, check: () =>
        {
            Args.Domains = Args.Domains?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            return !Args.Domains.IsNullOrEmpty();
        }));
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        await conetxt.AccountAsync(Args.AccountKey, cancellationToken: token);
        var order = await conetxt.NewOrderAsync(Args.Domains, cancellationToken: token);
        var d = await order.GetResourceAsync(token);
        Console.WriteLine(order.Location);
        Console.WriteLine(JsonSerializer.Serialize(d, DefaultAcmeHttpClient.JsonSerializerOptions));
        Console.WriteLine();
    }
}

public class NewOrderCommandOptions : AccountCommandOptions
{
    public string[] Domains { get; set; }
}

public class OrderCommandOptions : AccountCommandOptions
{
    public Uri Order { get; set; }

    public static void AddCommonArgs<T>(ArgsCommand<T> command) where T : OrderCommandOptions, new()
    {
        command.AddArg(new CommandArg("order", "o", null, $"The URI of the certificate order.", s =>
        {
            command.Args.Order = new Uri(s);
        }, check: () =>
        {
            return command.Args.Order != null;
        }));
        AccountCommandOptions.AddCommonArgs(command);
    }
}
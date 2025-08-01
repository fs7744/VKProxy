﻿using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace VKProxy.CommandLine;

internal class AccountCommand : CommandGroup
{
    public AccountCommand() : base("account", "Manage ACME accounts.")
    {
        Add(new NewAccountKeyCommand());
        Add(new NewAccountCommand());
        Add(new UpdateAccountCommand());
        Add(new CheckAccountCommand());
        Add(new DeactivateAccountCommand());
        Add(new ChangeAccountKeyCommand());
    }
}

public class AccountCommandOptions : ACMECommandOptions
{
    public Key AccountKey { get; set; }

    public static void AddCommonArgs<T>(ArgsCommand<T> command) where T : AccountCommandOptions, new()
    {
        command.AddArg(new CommandArg("key", "k", null, $"Account key path", s =>
        {
            var ss = File.ReadAllText(s);
            try
            {
                command.Args.AccountKey = KeyAlgorithmProvider.GetKey(ss);
            }
            catch (Exception)
            {
                var bytes = File.ReadAllBytes(s);
                command.Args.AccountKey = KeyAlgorithmProvider.GetKey(bytes);
            }
        }, check: () => command.Args.AccountKey != null));
        ACMECommandOptions.AddCommonArgs(command);
    }
}
namespace VKProxy.CommandLine;

public class ACMECommand : CommandGroup
{
    public ACMECommand() : base("acme", "cli tool to help you issues certificates through an automated API based on the ACME protocol.")
    {
        Add(new TermsCommand());
        Add(new AccountCommand());
    }
}
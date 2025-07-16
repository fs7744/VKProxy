using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace VKProxy.CommandLine;

internal class NewAccountKeyCommand : ArgsCommand<NewAccountKeyCommandOptions>
{
    public NewAccountKeyCommand() : base("key", "Create new account key file.")
    {
        AddArg(new CommandArg("algorithm", null, null, "support RS256/ES256/ES384/ES512", s => Args.Algorithm = Enum.Parse<KeyAlgorithm>(s, true)));
        AddArg(new CommandArg("output", null, null, "output file path", s => Args.Output = s));
        AddArg(new CommandArg("format", null, null, "key file format, support pem/der", s =>
        {
            if (string.IsNullOrWhiteSpace(s) || (!s.Equals("der", StringComparison.OrdinalIgnoreCase) && !s.Equals("pem", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("format", "key file format only support pem/der");
            Args.Format = s;
        }));
        AddArg(new CommandArg("key-size", null, null, "key size for rsa", s =>
        {
            if (string.IsNullOrWhiteSpace(s) || !int.TryParse(s, out var i) || i <= 0)
                throw new ArgumentException("key-size", "must be int");
            Args.KeySize = i;
        }));
    }

    protected override Task ExecAsync()
    {
        var key = KeyAlgorithmProvider.NewKey(Args.Algorithm, Args.KeySize);
        if (File.Exists(Args.Output))
            File.Delete(Args.Output);
        if (Args.Format.Equals("der", StringComparison.OrdinalIgnoreCase))
        {
            return File.WriteAllBytesAsync(Args.Output, key.ToDer());
        }
        else
            return File.WriteAllTextAsync(Args.Output, key.ToPem());
    }
}

public class NewAccountKeyCommandOptions
{
    public int? KeySize { get; set; }
    public string Output { get; set; } = "key";
    public string Format { get; set; } = "pem";
    public KeyAlgorithm Algorithm { get; set; } = KeyAlgorithm.RS256;
}
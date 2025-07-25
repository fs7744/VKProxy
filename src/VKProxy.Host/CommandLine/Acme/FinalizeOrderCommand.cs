using System.Security.Cryptography.X509Certificates;
using System.Text;
using VKProxy.ACME;
using VKProxy.Core.Extensions;

namespace VKProxy.CommandLine;

internal class FinalizeOrderCommand : ArgsCommand<FinalizeOrderCommandOptions>
{
    public FinalizeOrderCommand() : base("finalize", "Finalize an order.")
    {
        AuthzOrderCommandOptions.AddCommonArgs(this);
        AddArg(new CommandArg("algorithm", null, null, "support RS256/ES256/ES384/ES512", s => Args.Algorithm = Enum.Parse<KeyAlgorithm>(s, true)));
        AddArg(new CommandArg("output", null, null, "output file path, default is cert", s => Args.Output = s));
        AddArg(new CommandArg("format", null, null, "key file format, support pem/pfx", s =>
        {
            if (string.IsNullOrWhiteSpace(s) || (!s.Equals("pfx", StringComparison.OrdinalIgnoreCase) && !s.Equals("pem", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("format", "key file format only support pem/pfx");
            Args.Format = s;
        }));
        AddArg(new CommandArg("key-size", null, null, "key size for rsa", s =>
        {
            if (string.IsNullOrWhiteSpace(s) || !int.TryParse(s, out var i) || i <= 0)
                throw new ArgumentException("key-size", "must be int");
            Args.KeySize = i;
        }));
        AddArg(new CommandArg("additional-issuer", null, null, "additional issuer", s => Args.AdditionalIssuer = s));
    }

    protected override async Task ExecAsync()
    {
        var s = Args.GetCancellationTokenSource();
        var token = s.Token;
        var conetxt = await Args.GetAcmeContextAsync(token);
        await conetxt.AccountAsync(Args.AccountKey, cancellationToken: token);
        var order = conetxt.Order(Args.Order);
        var csrInfo = new CsrInfo
        {
            CommonName = Args.Domain,
        };
        Key privateKey = Args.Algorithm.NewKey(Args.KeySize);
        var acmeCert = await order.GenerateAsync(csrInfo, privateKey, cancellationToken: token);
        var pfxBuilder = acmeCert.ToPfx(privateKey);
        if (!string.IsNullOrWhiteSpace(Args.AdditionalIssuer) && File.Exists(Args.AdditionalIssuer))
        {
            pfxBuilder.AddIssuer(File.ReadAllBytes(Args.AdditionalIssuer));
        }
        var pfx = pfxBuilder.Build("HTTPS Cert - " + Args.Domain, string.Empty);
        var r = X509CertificateLoader.LoadPkcs12(pfx, string.Empty, X509KeyStorageFlags.Exportable);
        var path = $"{Args.Output}.{Args.Format}";
        if ("pfx".Equals(Args.Format, StringComparison.OrdinalIgnoreCase))
        {
            await File.WriteAllBytesAsync(path, r.Export(X509ContentType.Pfx), token);
        }
        else
        {
            await File.WriteAllTextAsync(path, r.ExportPem(), token);
        }
        Console.WriteLine(path);
        Console.WriteLine(r.ExportPem());
    }
}

public class FinalizeOrderCommandOptions : AuthzOrderCommandOptions
{
    public int? KeySize { get; set; }
    public string Output { get; set; } = "cert";
    public string Format { get; set; } = "pem";
    public string AdditionalIssuer { get; set; }
    public KeyAlgorithm Algorithm { get; set; } = KeyAlgorithm.RS256;
}
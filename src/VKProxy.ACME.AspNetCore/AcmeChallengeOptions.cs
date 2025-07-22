namespace VKProxy.ACME.AspNetCore;

public class AcmeChallengeOptions
{
    public Uri Server { get; set; } = WellKnownServers.LetsEncryptV2;

    public TimeSpan? Timeout { get; set; } = TimeSpan.FromMinutes(5);

    internal Func<IAcmeContext, Task<IAccountContext>> AccountFunc { get; set; }

    private (CancellationToken, CancellationTokenSource) Token()
    {
        if (Timeout.HasValue)
        {
            var s = new CancellationTokenSource(Timeout.Value);
            return (s.Token, s);
        }
        else
            return (CancellationToken.None, null);
    }

    public void Account(string pem)
    {
        Key accountKey = pem;
        AccountFunc = c =>
        {
            var (t, s) = Token();
            return c.AccountAsync(accountKey, t);
        };
    }

    public void Account(byte[] der)
    {
        Key accountKey = der;
        AccountFunc = c =>
        {
            var (t, s) = Token();
            return c.AccountAsync(accountKey, t);
        };
    }

    public void AccountFromFile(string path)
    {
        Key accountKey;
        try
        {
            accountKey = File.ReadAllText(path);
        }
        catch (Exception)
        {
            accountKey = File.ReadAllBytes(path);
        }
        AccountFunc = c =>
        {
            var (t, s) = Token();
            return c.AccountAsync(accountKey, t);
        };
    }

    public void NewAccount(IList<string> contact, KeyAlgorithm algorithm = KeyAlgorithm.RS256, int? keySize = null, string eabKeyId = null, string eabKey = null, string eabKeyAlg = null)
    {
        Key accountKey = algorithm.NewKey(keySize);
        AccountFunc = c =>
        {
            var (t, s) = Token();
            return c.NewAccountAsync(contact, true, accountKey, eabKeyId, eabKey, eabKeyAlg, t);
        };
    }
}
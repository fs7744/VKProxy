using DotNext;

namespace VKProxy.ACME.AspNetCore;

public class AcmeChallengeOptions
{
    public Uri Server { get; set; } = WellKnownServers.LetsEncryptV2;

    public string[] DomainNames { get; set; }

    /// <summary>
    /// How long before certificate expiration will be renewal attempted.
    /// Set to <c>null</c> to disable automatic renewal.
    /// </summary>
    public TimeSpan? RenewDaysInAdvance { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How often will be certificates checked for renewal
    /// </summary>
    public TimeSpan? RenewalCheckPeriod { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Specifies which kinds of ACME challenges LettuceEncrypt can use to verify domain ownership.
    /// Defaults to <see cref="ChallengeType.Any"/>.
    /// </summary>
    public ChallengeType AllowedChallengeTypes { get; set; } = ChallengeType.Any;

    internal Func<IAcmeContext, CancellationToken, Task<IAccountContext>> AccountFunc { get; set; }
    internal bool CanNewAccount { get; set; }

    internal void Check()
    {
        if (Server == null)
            throw new ArgumentNullException(nameof(Server));

        DomainNames = DomainNames?.Where(static i => !string.IsNullOrWhiteSpace(i))?.ToArray();

        if (DomainNames.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(DomainNames));
    }

    public void Account(string pem)
    {
        Key accountKey = pem;
        InitAccount(accountKey);
    }

    private void InitAccount(Key accountKey)
    {
        CanNewAccount = false;
        AccountFunc = async (c, t) =>
        {
            await c.InitAsync(Server, t);
            return await c.AccountAsync(accountKey, t);
        };
    }

    public void Account(byte[] der)
    {
        Key accountKey = der;
        InitAccount(accountKey);
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
        InitAccount(accountKey);
    }

    public void NewAccount(IList<string> contact, KeyAlgorithm algorithm = KeyAlgorithm.RS256, int? keySize = null, string eabKeyId = null, string eabKey = null, string eabKeyAlg = null)
    {
        CanNewAccount = true;
        AccountFunc = async (c, t) =>
        {
            Key accountKey = algorithm.NewKey(keySize);
            await c.InitAsync(Server, t);
            var r = await c.NewAccountAsync(contact, true, accountKey, eabKeyId, eabKey, eabKeyAlg, t);
            return r;
        };
    }
}
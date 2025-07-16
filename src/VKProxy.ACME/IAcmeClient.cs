using System.Security.Cryptography;
using System.Text.Json;
using VKProxy.ACME.Crypto;
using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IAcmeClient
{
    Task<AcmeDirectory?> DirectoryAsync(Uri directoryUri, CancellationToken cancellationToken = default);

    Task<AcmeResponse<Account>> NewAccountAsync(AcmeDirectory directory, Account account, IKey accountKey, Func<CancellationToken, Task<string>> consumeNonce, string eabKeyId = null, string eabKey = null, string eabKeyAlg = null, int retryCount = 1, CancellationToken cancellationToken = default);

    Task<AcmeResponse<string>> NewNonceAsync(AcmeDirectory directory, CancellationToken cancellationToken = default);
}

public class AcmeClient : IAcmeClient
{
    private readonly IAcmeHttpClient httpClient;

    public AcmeClient(IAcmeHttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<AcmeDirectory?> DirectoryAsync(Uri directoryUri, CancellationToken cancellationToken = default)
    {
        var data = await httpClient.GetAsync<AcmeDirectory>(directoryUri, cancellationToken);
        return data.Resource;
    }

    public Task<AcmeResponse<string>> NewNonceAsync(AcmeDirectory directory, CancellationToken cancellationToken = default)
    {
        return httpClient.HeadAsync<string>(directory.NewNonce, cancellationToken);
    }

    public Task<AcmeResponse<Account>> NewAccountAsync(AcmeDirectory directory, Account account, IKey accountKey, Func<CancellationToken, Task<string>> consumeNonce, string eabKeyId = null, string eabKey = null, string eabKeyAlg = null, int retryCount = 1, CancellationToken cancellationToken = default)
    {
        var endpoint = directory.NewAccount;
        var jws = new JwsSigner(accountKey);
        if (eabKeyId != null && eabKey != null)
        {
            var header = new
            {
                alg = eabKeyAlg?.ToUpper() ?? "HS256",
                kid = eabKeyId,
                url = endpoint
            };

            var headerJson = JsonSerializer.Serialize(header, DefaultAcmeHttpClient.JsonSerializerOptions);
            var protectedHeaderBase64 = JwsConvert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(headerJson));

            var accountKeyBase64 = JwsConvert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(accountKey.JsonWebKey, DefaultAcmeHttpClient.JsonSerializerOptions)
                    )
                );

            var signingBytes = System.Text.Encoding.ASCII.GetBytes($"{protectedHeaderBase64}.{accountKeyBase64}");

            // eab signature is the hash of the header and account key, using the eab key
            byte[] signatureHash;

            switch (header.alg)
            {
                case "HS512":
                    using (var hs512 = new HMACSHA512(JwsConvert.FromBase64String(eabKey))) signatureHash = hs512.ComputeHash(signingBytes);
                    break;

                case "HS384":
                    using (var hs384 = new HMACSHA384(JwsConvert.FromBase64String(eabKey))) signatureHash = hs384.ComputeHash(signingBytes);
                    break;

                default:
                    using (var hs256 = new HMACSHA256(JwsConvert.FromBase64String(eabKey))) signatureHash = hs256.ComputeHash(signingBytes);
                    break;
            }

            var signatureBase64 = JwsConvert.ToBase64String(signatureHash);

            account.ExternalAccountBinding = new
            {
                Protected = protectedHeaderBase64,
                Payload = accountKeyBase64,
                Signature = signatureBase64
            };
        }

        return httpClient.PostAsync<Account>(jws, endpoint, account, consumeNonce, retryCount, cancellationToken);
    }
}
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Primitives;
using System.Text;
using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing.SessionAffinity;

internal class CustomHeaderSessionAffinityPolicy : SessionAffinityPolicyBase
{
    private readonly string headerName;
    private readonly IDataProtector dataProvider;

    public CustomHeaderSessionAffinityPolicy(ILoadBalancingPolicy policy, string headerName, IDataProtector dataProtectionProvider) : base(policy)
    {
        this.headerName = headerName;
        this.dataProvider = dataProtectionProvider;
    }

    public override string Name => "CustomHeader";

    protected override bool DestinationEquals(string key, DestinationState destination)
    {
        return key.Equals(destination.Address, StringComparison.OrdinalIgnoreCase);
    }

    protected override string? GetRequestAffinityKey(IL7ReverseProxyFeature l7)
    {
        var headers = l7.Http.Request.Headers[headerName];
        if (StringValues.IsNullOrEmpty(headers)) return null;
        return Unprotect(dataProvider, headers[0]);
    }

    protected override void SetRequestAffinityKey(IL7ReverseProxyFeature l7, DestinationState destination)
    {
        l7.Http.Response.Headers[headerName] = Protect(dataProvider, destination.Address);
    }

    internal static string Protect(IDataProtector dataProvider, string unencryptedKey)
    {
        if (string.IsNullOrEmpty(unencryptedKey))
        {
            return unencryptedKey;
        }

        var userData = Encoding.UTF8.GetBytes(unencryptedKey);

        var protectedData = dataProvider.Protect(userData);
        return Convert.ToBase64String(protectedData).TrimEnd('=');
    }

    internal static string Unprotect(IDataProtector dataProvider, string? encryptedRequestKey)
    {
        if (string.IsNullOrEmpty(encryptedRequestKey))
        {
            return null;
        }

        try
        {
            var keyBytes = Convert.FromBase64String(Pad(encryptedRequestKey));

            var decryptedKeyBytes = dataProvider.Unprotect(keyBytes);
            if (decryptedKeyBytes is null)
            {
                return null;
            }

            return Encoding.UTF8.GetString(decryptedKeyBytes);
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    internal static string Pad(string text)
    {
        var padding = 3 - ((text.Length + 3) % 4);
        if (padding == 0)
        {
            return text;
        }
        return text + new string('=', padding);
    }
}
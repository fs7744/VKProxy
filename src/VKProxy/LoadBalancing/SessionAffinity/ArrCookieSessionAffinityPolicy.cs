using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing.SessionAffinity;

public class ArrCookieSessionAffinityPolicy : SessionAffinityPolicyBase
{
    private static readonly ConditionalWeakTable<DestinationState, string> hashes = new();
    private readonly SessionAffinityCookieOptions options;

    public ArrCookieSessionAffinityPolicy(ILoadBalancingPolicy policy, SessionAffinityCookieOptions options) : base(policy)
    {
        this.options = options;
    }

    public override string Name => "ArrCookie";

    public static string Hash(DestinationState d)
    {
        var destinationIdBytes = Encoding.Unicode.GetBytes(d.Address.ToLowerInvariant());
        var hashBytes = SHA256.HashData(destinationIdBytes);
        return Convert.ToHexString(hashBytes);
    }

    protected override bool DestinationEquals(string key, DestinationState destination)
    {
        return key == hashes.GetValue(destination, Hash);
    }

    protected override string? GetRequestAffinityKey(IL7ReverseProxyFeature l7)
    {
        return l7.Http.Request.Cookies[options.Name];
    }

    protected override void SetRequestAffinityKey(IL7ReverseProxyFeature l7, DestinationState? destination)
    {
        l7.Http.Response.Cookies.Append(options.Name, hashes.GetValue(destination, Hash), options.Create());
    }
}
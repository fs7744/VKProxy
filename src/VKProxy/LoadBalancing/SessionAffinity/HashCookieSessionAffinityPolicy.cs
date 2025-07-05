using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Text;
using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing.SessionAffinity;

internal class HashCookieSessionAffinityPolicy : SessionAffinityPolicyBase
{
    private static readonly ConditionalWeakTable<DestinationState, string> hashes = new();
    private readonly SessionAffinityCookieOptions options;

    public HashCookieSessionAffinityPolicy(ILoadBalancingPolicy policy, SessionAffinityCookieOptions options) : base(policy)
    {
        this.options = options;
    }

    public override string Name => "HashCookie";

    public static string Hash(DestinationState d)
    {
        var destinationIdBytes = Encoding.Unicode.GetBytes(d.Address.ToUpperInvariant());
        var hashBytes = XxHash64.Hash(destinationIdBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
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
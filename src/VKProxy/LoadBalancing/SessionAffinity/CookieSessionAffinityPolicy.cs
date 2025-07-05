using Microsoft.AspNetCore.DataProtection;
using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing.SessionAffinity;

public class CookieSessionAffinityPolicy : SessionAffinityPolicyBase
{
    private readonly SessionAffinityCookieOptions options;
    private readonly IDataProtector dataProvider;

    public CookieSessionAffinityPolicy(ILoadBalancingPolicy policy, SessionAffinityCookieOptions options, IDataProtector dataProtectionProvider) : base(policy)
    {
        this.options = options;
        this.dataProvider = dataProtectionProvider;
    }

    public override string Name => "Cookie";

    protected override bool DestinationEquals(string key, DestinationState destination)
    {
        return key.Equals(destination.Address, StringComparison.OrdinalIgnoreCase);
    }

    protected override string? GetRequestAffinityKey(IL7ReverseProxyFeature l7)
    {
        return CustomHeaderSessionAffinityPolicy.Unprotect(dataProvider, l7.Http.Request.Cookies[options.Name]);
    }

    protected override void SetRequestAffinityKey(IL7ReverseProxyFeature l7, DestinationState? destination)
    {
        l7.Http.Response.Cookies.Append(options.Name, CustomHeaderSessionAffinityPolicy.Protect(dataProvider, destination.Address), options.Create());
    }
}
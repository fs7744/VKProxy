using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing.SessionAffinity;

public class SessionAffinityLoadBalancingPolicy : ILoadBalancingPolicy
{
    private readonly IDataProtector dataProtectionProvider;

    public string Name => "SessionAffinity";

    public SessionAffinityLoadBalancingPolicy(IDataProtectionProvider dataProtectionProvider)
    {
        this.dataProtectionProvider = dataProtectionProvider.CreateProtector(GetType().FullName);
    }

    public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
    {
        return availableDestinations[Random.Shared.Next(availableDestinations.Count)];
    }

    public void Init(ClusterConfig cluster)
    {
        if (cluster.Metadata is null || cluster.LoadBalancingPolicyInstance == null) return;
        if (cluster.Metadata.TryGetValue("SessionAffinity", out var way) && !string.IsNullOrWhiteSpace(way))
        {
            if (way.Equals("CustomHeader", StringComparison.OrdinalIgnoreCase))
            {
                string headerName;
                if (!cluster.Metadata.TryGetValue("SessionAffinityKey", out headerName) || string.IsNullOrWhiteSpace(headerName))
                {
                    headerName = "x-sessionaffinity";
                }
                cluster.LoadBalancingPolicyInstance = new CustomHeaderSessionAffinityPolicy(cluster.LoadBalancingPolicyInstance, headerName, dataProtectionProvider);
                return;
            }

            var cookie = new SessionAffinityCookieOptions();
            if (!cluster.Metadata.TryGetValue("SessionAffinityKey", out var v) || string.IsNullOrWhiteSpace(v))
            {
                v = "SessionAffinity";
            }
            cookie.Name = v;
            if (!cluster.Metadata.TryGetValue("CookiePath", out v) || string.IsNullOrWhiteSpace(v))
            {
                v = "/";
            }
            cookie.Options.Path = v;
            if (!cluster.Metadata.TryGetValue("CookieDomain", out v) || string.IsNullOrWhiteSpace(v))
            {
                v = null;
            }
            cookie.Options.Domain = v;
            if (!cluster.Metadata.TryGetValue("CookieHttpOnly", out v) || string.IsNullOrWhiteSpace(v) || !bool.TryParse(v, out var b))
            {
                b = true;
            }
            cookie.Options.HttpOnly = b;
            if (!cluster.Metadata.TryGetValue("CookieIsEssential", out v) || string.IsNullOrWhiteSpace(v) || !bool.TryParse(v, out b))
            {
                b = false;
            }
            cookie.Options.IsEssential = b;
            if (!cluster.Metadata.TryGetValue("CookieSecure", out v) || string.IsNullOrWhiteSpace(v) || !bool.TryParse(v, out b))
            {
                b = false;
            }
            cookie.Options.Secure = b;
            if (!cluster.Metadata.TryGetValue("CookieExpires", out v) || string.IsNullOrWhiteSpace(v) || !TimeSpan.TryParse(v, out var t))
            {
                cookie.Expires = null;
            }
            else
                cookie.Expires = t;
            if (!cluster.Metadata.TryGetValue("CookieMaxAge", out v) || string.IsNullOrWhiteSpace(v) || !TimeSpan.TryParse(v, out t))
            {
                cookie.Options.MaxAge = null;
            }
            else
                cookie.Options.MaxAge = t;

            if (!cluster.Metadata.TryGetValue("CookieSameSite", out v) || string.IsNullOrWhiteSpace(v) || !Enum.TryParse<SameSiteMode>(v, true, out var e))
            {
                e = SameSiteMode.Unspecified;
            }
            cookie.Options.SameSite = e;

            if (way.Equals("HashCookie", StringComparison.OrdinalIgnoreCase))
            {
                cluster.LoadBalancingPolicyInstance = new HashCookieSessionAffinityPolicy(cluster.LoadBalancingPolicyInstance, cookie);
                return;
            }
            else if (way.Equals("ArrCookie", StringComparison.OrdinalIgnoreCase))
            {
                cluster.LoadBalancingPolicyInstance = new ArrCookieSessionAffinityPolicy(cluster.LoadBalancingPolicyInstance, cookie);
                return;
            }
            else if (way.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
            {
                cluster.LoadBalancingPolicyInstance = new CookieSessionAffinityPolicy(cluster.LoadBalancingPolicyInstance, cookie, dataProtectionProvider);
                return;
            }
        }
    }
}
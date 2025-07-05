using VKProxy.Config;
using VKProxy.Features;

namespace VKProxy.LoadBalancing.SessionAffinity;

public abstract class SessionAffinityPolicyBase : ILoadBalancingPolicy
{
    protected readonly ILoadBalancingPolicy policy;
    public abstract string Name { get; }

    public SessionAffinityPolicyBase(ILoadBalancingPolicy policy)
    {
        this.policy = policy;
    }

    public virtual void Init(ClusterConfig cluster)
    {
    }

    public virtual DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
    {
        if (feature is not IL7ReverseProxyFeature l7)
        {
            return policy.PickDestination(feature, availableDestinations);
        }
        var key = GetRequestAffinityKey(l7);
        DestinationState r = null;
        var reNew = true;
        if (!string.IsNullOrEmpty(key))
        {
            for (var i = 0; i < availableDestinations.Count; i++)
            {
                var j = availableDestinations[i];
                if (DestinationEquals(key, j))
                {
                    r = j;
                    reNew = false;
                    break;
                }
            }
        }

        if (reNew)
        {
            r = policy.PickDestination(feature, availableDestinations);
            SetRequestAffinityKey(l7, r);
        }
        return r;
    }

    protected abstract bool DestinationEquals(string key, DestinationState destination);

    protected abstract void SetRequestAffinityKey(IL7ReverseProxyFeature l7, DestinationState? destination);

    protected abstract string? GetRequestAffinityKey(IL7ReverseProxyFeature l7);
}
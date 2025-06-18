using VKProxy.Config;
using VKProxy.Core.Infrastructure;
using VKProxy.Features;

namespace VKProxy.LoadBalancing;

public sealed class HashLoadBalancingPolicy : ILoadBalancingPolicy
{
    public string Name => LoadBalancingPolicy.Hash;

    public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
    {
        return availableDestinations[Random.Shared.Next(availableDestinations.Count)];
    }

    public void Init(ClusterConfig cluster)
    {
        if (cluster.Metadata is null) return;
        if (cluster.Metadata.TryGetValue("HashBy", out var hashBy) && !string.IsNullOrWhiteSpace(hashBy))
        {
            if ("header".Equals(hashBy, StringComparison.OrdinalIgnoreCase))
            {
                if (cluster.Metadata.TryGetValue("Key", out var key) && !string.IsNullOrWhiteSpace(key))
                {
                    cluster.LoadBalancingPolicyInstance = new HashByHeader(key);
                }
            }
            else if ("cookie".Equals(hashBy, StringComparison.OrdinalIgnoreCase))
            {
                if (cluster.Metadata.TryGetValue("Key", out var key) && !string.IsNullOrWhiteSpace(key))
                {
                    cluster.LoadBalancingPolicyInstance = new HashByCookie(key);
                }
            }
            else if ("items".Equals(hashBy, StringComparison.OrdinalIgnoreCase))
            {
                if (cluster.Metadata.TryGetValue("Key", out var key) && !string.IsNullOrWhiteSpace(key))
                {
                    cluster.LoadBalancingPolicyInstance = new HashByItems(key);
                }
            }
        }
    }

    private sealed class HashByHeader : ILoadBalancingPolicy
    {
        private string key;

        public HashByHeader(string key)
        {
            this.key = key;
        }

        public string Name => LoadBalancingPolicy.Hash;

        public void Init(ClusterConfig cluster)
        {
        }

        public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
        {
            DestinationState r = null;

            if (feature is IL7ReverseProxyFeature l7)
            {
                var k = l7.Http.Request.Headers[key].ToString();
                var c = Math.Abs(StringHashing.HashOrdinalIgnoreCase(k));
                return availableDestinations[c % availableDestinations.Count];
            }

            if (r == null)
            {
                r = availableDestinations[Random.Shared.Next(availableDestinations.Count)];
            }
            return r;
        }
    }

    private sealed class HashByCookie : ILoadBalancingPolicy
    {
        private string key;

        public HashByCookie(string key)
        {
            this.key = key;
        }

        public string Name => LoadBalancingPolicy.Hash;

        public void Init(ClusterConfig cluster)
        {
        }

        public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
        {
            DestinationState r = null;

            if (feature is IL7ReverseProxyFeature l7)
            {
                var k = l7.Http.Request.Cookies[key]?.ToString();
                if (k != null)
                {
                    var c = Math.Abs(StringHashing.HashOrdinalIgnoreCase(k));
                    return availableDestinations[c % availableDestinations.Count];
                }
            }

            if (r == null)
            {
                r = availableDestinations[Random.Shared.Next(availableDestinations.Count)];
            }
            return r;
        }
    }

    private sealed class HashByItems : ILoadBalancingPolicy
    {
        private string key;

        public HashByItems(string key)
        {
            this.key = key;
        }

        public string Name => LoadBalancingPolicy.Hash;

        public void Init(ClusterConfig cluster)
        {
        }

        public DestinationState? PickDestination(IReverseProxyFeature feature, IReadOnlyList<DestinationState> availableDestinations)
        {
            DestinationState r = null;

            if (feature is IL7ReverseProxyFeature l7)
            {
                var k = l7.Http.Items[key]?.ToString();
                if (k != null)
                {
                    var c = Math.Abs(StringHashing.HashOrdinalIgnoreCase(k));
                    return availableDestinations[c % availableDestinations.Count];
                }
            }

            if (r == null)
            {
                r = availableDestinations[Random.Shared.Next(availableDestinations.Count)];
            }
            return r;
        }
    }
}
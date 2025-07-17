using VKProxy.ACME.Resource;

namespace VKProxy.ACME;

public interface IOrderContext : IResourceContext<Order>
{
}

internal class OrderContext : ResourceContext<Order>, IOrderContext
{
    public OrderContext(IAcmeContext context, Uri location) : base(context, location)
    {
    }
}
using System.Collections;
using VKProxy.Config;

namespace VKProxy.ServiceDiscovery;

public class UnionDestinationResolverState : IDestinationResolverState
{
    private IEnumerable<IDestinationResolverState> destinations;

    public UnionDestinationResolverState(IEnumerable<IDestinationResolverState> destinations)
    {
        this.destinations = destinations;
    }

    public DestinationState this[int index]
    {
        get
        {
            foreach (var item in destinations)
            {
                if (index < item.Count)
                {
                    return item[index];
                }
                index -= item.Count;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public int Count => destinations.Sum(i => i.Count);

    public void Dispose()
    {
        var old = destinations;
        destinations = null;
        if (old != null)
        {
            foreach (var item in old)
            {
                item.Dispose();
            }
        }
    }

    public IEnumerator<DestinationState> GetEnumerator()
    {
        return destinations.SelectMany(i => i).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
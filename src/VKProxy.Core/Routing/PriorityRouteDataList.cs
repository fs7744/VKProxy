namespace VKProxy.Core.Routing;

public class PriorityRouteDataList<T> : SortedDictionary<int, List<T>>
{
    public static readonly OrderComparer OrderComparer = new OrderComparer();

    public PriorityRouteDataList() : base(OrderComparer)
    {
    }
}

public class OrderComparer : IComparer<int>
{
    public int Compare(int x, int y)
    {
        return y.CompareTo(x);
    }
}
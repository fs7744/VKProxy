namespace VKProxy.Core.Routing;

public class PriorityRouteDataList<T> : SortedDictionary<int, List<T>>
{
    public PriorityRouteDataList() : base(OrderComparer.Default)
    {
    }
}

public class OrderComparer : IComparer<int>
{
    public static readonly OrderComparer Default = new OrderComparer();
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    public int Compare(int x, int y)
    {
        return y.CompareTo(x);
    }
}
using DotNext.Collections.Generic;
using System.Collections.Frozen;

namespace VKProxy.Core.Routing;

public class RouteTableBuilder<T>
{
    private readonly RadixTrie<PriorityRouteDataList<T>> trie;
    private readonly StringComparison comparison;
    private readonly int cacheSize;
    private Dictionary<string, PriorityRouteDataList<T>> exact;

    private IEqualityComparer<string>? MatchComparison(StringComparison comparison)
    {
        return comparison switch
        {
            StringComparison.CurrentCulture => StringComparer.CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
            StringComparison.InvariantCulture => StringComparer.InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
            StringComparison.Ordinal => StringComparer.Ordinal,
            StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
        };
    }

    public RouteTableBuilder(StringComparison comparison = StringComparison.Ordinal, int cacheSize = 1024)
    {
        this.comparison = comparison;
        this.cacheSize = cacheSize;
        exact = new Dictionary<string, PriorityRouteDataList<T>>(MatchComparison(comparison));
        trie = new RadixTrie<PriorityRouteDataList<T>>();
    }

    public void Add(string key, T value, RouteType type, int priority = 0)
    {
        switch (type)
        {
            case RouteType.Exact:
                var list = exact.GetOrAdd(key, CreatePriorityRouteDataList);
                if (list.TryGetValue(priority, out var v))
                {
                    v.Add(value);
                }
                else
                {
                    v = new List<T> { value };
                    list.Add(priority, v);
                }
                break;

            case RouteType.Prefix:
                trie.Add(key, () => new PriorityRouteDataList<T>() { { priority, new List<T>() { value } } }, MergePriorityRouteDataList);
                break;
        }
    }

    private PriorityRouteDataList<T>? MergePriorityRouteDataList(PriorityRouteDataList<T>? list1, PriorityRouteDataList<T>? list2)
    {
        if (list1 is null) return list2;
        if (list2 is null) return list1;
        foreach (var item in list2)
        {
            if (list1.TryGetValue(item.Key, out var v))
            {
                v.AddAll(item.Value);
            }
            else
            {
                list1.Add(item.Key, item.Value);
            }
        }
        return list1;
    }

    private PriorityRouteDataList<T> CreatePriorityRouteDataList(string arg)
    {
        return new PriorityRouteDataList<T>();
    }

    public IRouteTable<T> Build(RouteTableType type)
    {
        return type == RouteTableType.OnlyFirst
            ? new OnlyFirstRouteTable<T>(exact.ToFrozenDictionary(MatchComparison(comparison)), trie, cacheSize, comparison)
            : new RouteTable<T>(exact.ToFrozenDictionary(MatchComparison(comparison)), trie, cacheSize, comparison);
    }
}
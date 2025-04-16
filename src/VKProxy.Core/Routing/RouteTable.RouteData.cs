using DotNext.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using VKProxy.Core.Infrastructure;

namespace VKProxy.Core.Routing;

public class PriorityRouteDataList<T, R> : SortedDictionary<int, R> where R : IRouteData<T>, new()
{
    public PriorityRouteDataList() : base(OrderComparer.Default)
    {
    }
}

public interface IRouteData<T>
{
    public void Add(T value);

    void Add(IRouteData<T> value);

    ValueTask<T> MatchAsync<R2>(string key, R2 data, Func<T, R2, bool> match);

    T Match<R2>(string key, R2 data, Func<T, R2, bool> match);

    void Init(int cacheSize);
}

public class RouteTableBuilder<T, R> where R : IRouteData<T>, new()
{
    private readonly RadixTrie<PriorityRouteDataList<T, R>> trie;
    private readonly StringComparison comparison;
    private readonly int cacheSize;
    private Dictionary<string, PriorityRouteDataList<T, R>> exact;

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
        exact = new Dictionary<string, PriorityRouteDataList<T, R>>(MatchComparison(comparison));
        trie = new RadixTrie<PriorityRouteDataList<T, R>>();
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
                    v = new R();
                    v.Add(value);
                    list.Add(priority, v);
                }
                break;

            case RouteType.Prefix:
                trie.Add(key, () =>
                {
                    var rv = new R();
                    rv.Add(value);
                    return new PriorityRouteDataList<T, R>() { { priority, rv } };
                }, MergePriorityRouteDataList);
                break;
        }
    }

    private PriorityRouteDataList<T, R>? MergePriorityRouteDataList(PriorityRouteDataList<T, R>? list1, PriorityRouteDataList<T, R>? list2)
    {
        if (list1 is null) return list2;
        if (list2 is null) return list1;
        foreach (var item in list2)
        {
            if (list1.TryGetValue(item.Key, out var v))
            {
                v.Add(item.Value);
            }
            else
            {
                list1.Add(item.Key, item.Value);
            }
        }
        return list1;
    }

    private PriorityRouteDataList<T, R> CreatePriorityRouteDataList(string arg)
    {
        return new PriorityRouteDataList<T, R>();
    }

    public RouteTable<T, R> Build(RouteTableType type)
    {
        exact.Values.SelectMany(static i => i.Values).Union(trie.GetAll().SelectMany(static i => i.Values)).ForEach(i => i.Init(cacheSize));
        return new RouteTable<T, R>(exact.ToFrozenDictionary(MatchComparison(comparison)), trie, comparison);
    }
}

public class RouteTable<T, R> where R : IRouteData<T>, new()
{
    private RadixTrie<PriorityRouteDataList<T, R>> trie;
    private readonly StringComparison comparison;
    private FrozenDictionary<string, R[]> exact;
    private ConcurrentDictionary<string, R[]> cache;

    public RouteTable(IDictionary<string, PriorityRouteDataList<T, R>> exact, RadixTrie<PriorityRouteDataList<T, R>> trie, StringComparison comparison)
    {
        this.trie = trie;
        this.comparison = comparison;
        var c = CollectionUtilities.MatchComparison(comparison);
        this.exact = exact.ToFrozenDictionary(i => i.Key, i => i.Value.Select(j => j.Value).ToArray(), c);
        cache = new ConcurrentDictionary<string, R[]>(c);
    }

    public async ValueTask<T> MatchAsync<R2>(string key, string key2, R2 data, Func<T, R2, bool> match)
    {
        if (trie == null) return default;
        if (exact.TryGetValue(key, out var result))
        {
            foreach (var rr in result)
            {
                var r = await rr.MatchAsync(key2, data, match);
                if (r is not null)
                    return r;
            }
        }
        else
        {
            foreach (var rr in cache.GetOrAdd(key, k => trie.Search(k, comparison).SelectMany(static i => i.Values.Select(static j => j)).ToArray()))
            {
                var r = await rr.MatchAsync(key2, data, match);
                if (r is not null)
                    return r;
            }
        }
        return default;
    }

    public T Match<R2>(string key, string key2, R2 data, Func<T, R2, bool> match)
    {
        if (trie == null) return default;
        if (exact.TryGetValue(key, out var result))
        {
            foreach (var rr in result.AsSpan())
            {
                var r = rr.Match(key2, data, match);
                if (r is not null)
                    return r;
            }
        }
        else
        {
            foreach (var rr in cache.GetOrAdd(key, k => trie.Search(k, comparison).SelectMany(static i => i.Values.Select(static j => j)).ToArray()).AsSpan())
            {
                var r = rr.Match(key2, data, match);
                if (r is not null)
                    return r;
            }
        }
        return default;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    public void Dispose()
    {
        if (trie != null)
        {
            var r = trie;
            trie = null;
            exact = null;
            cache = null;
            r?.Dispose();
        }
    }
}
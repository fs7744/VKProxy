using DotNext.Collections.Generic;
using DotNext.Runtime.Caching;
using System.Collections.Frozen;

using VKProxy.Core.Infrastructure;

namespace VKProxy.Core.Routing;

public class RouteTable<T> : IRouteTable<T>
{
    private RadixTrie<PriorityRouteDataList<T>> trie;
    private readonly StringComparison comparison;
    private RandomAccessCache<string, T[]> cache;
    private FrozenDictionary<string, T[]> exact;

    public RouteTable(IDictionary<string, PriorityRouteDataList<T>> exact, RadixTrie<PriorityRouteDataList<T>> trie, int cacheSize, StringComparison comparison)
    {
        cache = new RandomAccessCache<string, T[]>(cacheSize);
        this.trie = trie;
        this.comparison = comparison;
        this.exact = exact.ToFrozenDictionary(i => i.Key, i => i.Value.SelectMany(j => j.Value).ToArray(), CollectionUtilities.MatchComparison(comparison));
    }

    public async ValueTask<T> MatchAsync<R>(string key, R data, Func<T, R, bool> match)
    {
        if (trie == null) return default;
        var all = await FindAllAsync(key);
        if (all.Length == 0) return default;
        foreach (var v in all.AsSpan())
        {
            if (match(v, data))
            {
                return v;
            }
        }
        return default;
    }

    public async ValueTask<T> FirstAsync(string key)
    {
        var all = await FindAllAsync(key);
        if (all is null) return default;
        return all.FirstOrDefault();
    }

    public async ValueTask<T[]> FindAllAsync(string key)
    {
        if (cache.TryRead(key, out var session))
        {
            return session.Value;
        }
        else
        {
            using var writeSession = await cache.ChangeAsync(key);
            if (!writeSession.TryGetValue(out var value))
            {
                if (exact.TryGetValue(key, out var result))
                {
                    value = result;
                }
                else
                {
                    value = trie.Search(key, comparison).SelectMany(i => i.Values.SelectMany(j => j)).ToArray();
                    if (value.Length == 0)
                        value = Array.Empty<T>();
                }
                writeSession.SetValue(value);
            }

            return value;
        }
    }

    public T[] FindAll(string key)
    {
        if (cache.TryRead(key, out var session))
        {
            return session.Value;
        }
        else
        {
            using var writeSession = cache.Change(key, OrderComparer.Timeout);
            if (!writeSession.TryGetValue(out var value))
            {
                if (exact.TryGetValue(key, out var result))
                {
                    value = result;
                }
                else
                {
                    value = trie.Search(key, comparison).SelectMany(i => i.Values.SelectMany(j => j)).ToArray();
                    if (value.Length == 0)
                        value = Array.Empty<T>();
                }
                writeSession.SetValue(value);
            }

            return value;
        }
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
            var c = cache;
            cache = null;
            c?.Dispose();
            r?.Dispose();
        }
    }

    public T Match<R>(string key, R data, Func<T, R, bool> match)
    {
        if (trie == null) return default;
        var all = FindAll(key);
        if (all.Length == 0) return default;
        foreach (var v in all.AsSpan())
        {
            if (match(v, data))
            {
                return v;
            }
        }
        return default;
    }
}
using DotNext.Collections.Generic;
using DotNext.Runtime.Caching;
using System.Collections.Frozen;

using VKProxy.Core.Infrastructure;

namespace VKProxy.Core.Routing;

public class OnlyFirstRouteTable<T> : IRouteTable<T>
{
    private RadixTrie<PriorityRouteDataList<T>> trie;
    private readonly StringComparison comparison;
    private static RandomAccessCache<string, T> cache;
    private FrozenDictionary<string, T[]> exact;

    public OnlyFirstRouteTable(IDictionary<string, PriorityRouteDataList<T>> exact, RadixTrie<PriorityRouteDataList<T>> trie, int cacheSize, StringComparison comparison)
    {
        cache = new RandomAccessCache<string, T>(cacheSize) { KeyComparer = StringComparer.OrdinalIgnoreCase };
        this.trie = trie;
        this.comparison = comparison;
        this.exact = exact.ToFrozenDictionary(i => i.Key, i => i.Value.SelectMany(j => j.Value).ToArray(), CollectionUtilities.MatchComparison(comparison));
    }

    public async ValueTask<T> MatchAsync<R>(string key, R data, Func<T, R, bool> match)
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
                    value = DoMatch(result, data, match);
                }
                else
                {
                    value = DoMatchEnumerable(trie.Search(key, comparison).SelectMany(i => i.Values.SelectMany(j => j)), data, match);
                }
                writeSession.SetValue(value);
            }

            return value;
        }
    }

    public T Match<R>(string key, R data, Func<T, R, bool> match)
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
                    value = DoMatch(result, data, match);
                }
                else
                {
                    value = DoMatchEnumerable(trie.Search(key, comparison).SelectMany(i => i.Values.SelectMany(j => j)), data, match);
                }
                writeSession.SetValue(value);
            }

            return value;
        }
    }

    public T DoMatch<R>(T[] all, R data, Func<T, R, bool> match)
    {
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

    public T DoMatchEnumerable<R>(IEnumerable<T> all, R data, Func<T, R, bool> match)
    {
        foreach (var v in all)
        {
            if (match(v, data))
            {
                return v;
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
            var c = cache;
            cache = null;
            c?.Dispose();
            r?.Dispose();
        }
    }
}
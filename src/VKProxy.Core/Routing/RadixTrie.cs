namespace VKProxy.Core.Routing;

public class RadixTrieNode<T> : IDisposable
{
    public string Key;
    public T? Value;
    public List<RadixTrieNode<T>> Children;

    public void Dispose()
    {
        if (Children != null)
        {
            var c = Children;
            Children = null;
            c?.ForEach(x => x.Dispose());
            c?.Clear();
        }
    }
}

public class RadixTrie<T> : IDisposable
{
    private RadixTrieNode<T> trie;
    public T[] all;

    public RadixTrie()
    {
        trie = new RadixTrieNode<T>();
    }

    public void Add(string key, T value, Func<T?, T?, T?> merge)
    {
        Add(trie, key, () => value, merge);
    }

    public void Add(string key, Func<T> value, Func<T?, T?, T?> merge)
    {
        if (string.IsNullOrEmpty(key))
        {
            if (all == null)
            {
                all = [value()];
            }
            else
            {
                all[0] = merge(all[0], value());
            }
        }
        else
        {
            Add(trie, key, value, merge);
        }
    }

    public static void Add(RadixTrieNode<T> curr, string term, Func<T> value, Func<T?, T?, T?> merge)
    {
        int common = 0;
        if (curr.Children != null)
        {
            for (int j = 0; j < curr.Children.Count; j++)
            {
                var node = curr.Children[j];
                var key = node.Key;
                for (int i = 0; i < Math.Min(term.Length, key.Length); i++) if (term[i] == key[i]) common = i + 1; else break;

                if (common > 0)
                {
                    //term already existed
                    //existing ab
                    //new      ab
                    if ((common == term.Length) && (common == key.Length))
                    {
                        node.Value = merge(node.Value, value());
                    }//new is subkey
                     //existing abcd
                     //new      ab
                     //if new is shorter (== common), then node(count) and only 1. children add (clause2)
                    else if (common == term.Length)
                    {
                        node.Key = key.Substring(common);
                        var child = new RadixTrieNode<T>() { Key = term.Substring(0, common), Children = new List<RadixTrieNode<T>>() { node } };
                        child.Value = value();
                        curr.Children[j] = child;
                    }
                    //if oldkey shorter (==common), then recursive addTerm (clause1)
                    //existing: te
                    //new:      test
                    else if (common == key.Length)
                    {
                        Add(node, term.Substring(common), value, merge);
                    }
                    //old and new have common substrings
                    //existing: test
                    //new:      team
                    else
                    {
                        var child = new RadixTrieNode<T>() { Key = term.Substring(0, common) };
                        node.Key = key.Substring(common);
                        child.Children = new List<RadixTrieNode<T>>() { node, new RadixTrieNode<T>() { Key = term.Substring(common), Value = value() } };
                        curr.Children[j] = child;
                    }
                    return;
                }
            }
        }

        if (curr.Children is null)
        {
            curr.Children = new List<RadixTrieNode<T>>() { new RadixTrieNode<T>() { Key = term, Value = value() } };
        }
        else
        {
            curr.Children.Add(new RadixTrieNode<T>() { Key = term, Value = value() });
        }
    }

    public IEnumerable<T> Search(string key, StringComparison comparison = StringComparison.Ordinal)
    {
        if (all != null)
        {
            return Search(trie, new StringSegment(0, key), comparison).Union(all);
        }
        return Search(trie, new StringSegment(0, key), comparison);
    }

    private static IEnumerable<T> Search(RadixTrieNode<T> trie, StringSegment key, StringComparison comparison)
    {
        if (trie.Children is null) yield break;

        foreach (var item in trie.Children)
        {
            if (key.GetSpan.StartsWith(item.Key, comparison))
            {
                if (trie.Children != null)
                {
                    foreach (var item1 in Search(item, new StringSegment(key.Start + item.Key.Length, key.String), comparison))
                    {
                        yield return item1;
                    }
                }

                if (item.Value != null)
                {
                    yield return item.Value;
                }
            }
        }
    }

    public void Dispose()
    {
        if (trie != null)
        {
            var r = trie;
            trie = null;
            r?.Dispose();
        }
    }
}

internal readonly struct StringSegment
{
    public readonly int Start;
    public readonly string String;

    public StringSegment(int start, string str)
    {
        Start = start;
        String = str;
    }

    public ReadOnlySpan<char> GetSpan => String.AsSpan(Start);
}
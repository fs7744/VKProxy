namespace VKProxy.Core.Infrastructure;

public class CollectionUtilities
{
    public static IEqualityComparer<string>? MatchComparison(StringComparison comparison)
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

    public static bool EqualsString(IReadOnlyList<string>? list1, IReadOnlyList<string>? list2, StringComparer comparer = null)
    {
        return Equals(list1, list2, comparer ?? StringComparer.OrdinalIgnoreCase);
    }

    public static bool EqualsString(IReadOnlySet<string>? list1, IReadOnlySet<string>? list2, StringComparer comparer = null)
    {
        return Equals(list1, list2, comparer ?? StringComparer.OrdinalIgnoreCase);
    }

    public static bool Equals<T>(IReadOnlyList<T>? list1, IReadOnlyList<T>? list2, IEqualityComparer<T>? valueComparer = null)
    {
        if (ReferenceEquals(list1, list2))
        {
            return true;
        }

        if (list1 is null)
        {
            return list2 is null;
        }

        if (list2 is null)
        {
            return list1 is null;
        }

        if (list1.Count != list2.Count)
        {
            return false;
        }

        valueComparer ??= EqualityComparer<T>.Default;

        for (var i = 0; i < list1.Count; i++)
        {
            if (!valueComparer.Equals(list1[i], list2[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static bool Equals<T>(IReadOnlySet<T>? list1, IReadOnlySet<T>? list2, IEqualityComparer<T> comparer = null)
    {
        if (list1 is null)
        {
            return list2 is null;
        }

        if (list2 is null)
        {
            return list1 is null;
        }
        return Equals(list1.ToList(), list2.ToList(), comparer);
    }

    public static bool Equals<T>(IReadOnlyDictionary<string, T>? dictionary1, IReadOnlyDictionary<string, T>? dictionary2, IEqualityComparer<T>? valueComparer = null)
    {
        if (ReferenceEquals(dictionary1, dictionary2))
        {
            return true;
        }

        if (dictionary1 is null)
        {
            return dictionary2 is null;
        }

        if (dictionary2 is null)
        {
            return dictionary1 is null;
        }

        if (dictionary1.Count != dictionary2.Count)
        {
            return false;
        }

        if (dictionary1.Count == 0)
        {
            return true;
        }

        valueComparer ??= EqualityComparer<T>.Default;

        foreach (var (key, value1) in dictionary1)
        {
            if (dictionary2.TryGetValue(key, out var value2))
            {
                if (!valueComparer.Equals(value1, value2))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public static bool EqualsStringDict(IReadOnlyDictionary<string, string>? dictionary1, IReadOnlyDictionary<string, string>? dictionary2, IEqualityComparer<string>? valueComparer = null)
    {
        return Equals(dictionary1, dictionary2, valueComparer ?? StringComparer.Ordinal);
    }

    public static int GetStringHashCode(IEnumerable<string>? values, StringComparer comparer = null)
    {
        return GetHashCode(values, comparer ?? StringComparer.OrdinalIgnoreCase);
    }

    public static int GetHashCode<T>(IEnumerable<T>? values, IEqualityComparer<T>? valueComparer = null)
    {
        if (values is null)
        {
            return 0;
        }

        valueComparer ??= EqualityComparer<T>.Default;

        var hashCode = new HashCode();
        foreach (var value in values)
        {
            hashCode.Add(value, valueComparer);
        }
        return hashCode.ToHashCode();
    }

    public static int GetHashCode<T>(IReadOnlyDictionary<string, T>? dictionary, IEqualityComparer<T>? valueComparer = null)
    {
        if (dictionary is null)
        {
            return 0;
        }

        if (dictionary.Count == 0)
        {
            return 42;
        }

        // We don't know what comparer the dictionary was created with, so we assume it's Ordinal/OrdinalIgnoreCase
        // If a culture-sensitive comparer was used, this may result in GetHashCode returning different values for "equal" strings
        // If that comes up as a realistic scenario, we can consider ignoring keys in the future
        var keyComparer = StringComparer.OrdinalIgnoreCase;
        valueComparer ??= EqualityComparer<T>.Default;

        // Dictionaries are unordered collections and HashCode uses an order-sensitive algorithm (xxHash), so we have to sort the elements
        var keys = dictionary.Keys.ToArray();
        Array.Sort(keys, keyComparer);

        var hashCode = new HashCode();
        foreach (var key in keys)
        {
            hashCode.Add(key, keyComparer);
            hashCode.Add(dictionary[key], valueComparer);
        }
        return hashCode.ToHashCode();
    }
}
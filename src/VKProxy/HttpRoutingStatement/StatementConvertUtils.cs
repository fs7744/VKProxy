using System.Collections.Frozen;
using System.Linq;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement;

public static class StatementConvertUtils
{
    public static string ConvertToString(ValueStatement value)
    {
        if (value is StringValueStatement svs)
        {
            return svs.Value;
        }
        else if (value is NumberValueStatement nvs)
        {
            return nvs.Value.ToString();
        }
        else if (value is BooleanValueStatement bvs)
        {
            return bvs.Value.ToString();
        }
        else if (value is NullValueStatement)
        {
            return null;
        }

        return null;
    }

    public static FrozenSet<string> ConvertToString(ArrayValueStatement value)
    {
        if (value is StringArrayValueStatement svs && svs.Value != null)
        {
            return svs.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }
        else if (value is BooleanArrayValueStatement b && b.Value != null)
        {
            return b.Value.Select(static i => i.HasValue ? i.Value.ToString() : null).Distinct(StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }
        else if (value is NumberArrayValueStatement n && n.Value != null)
        {
            return n.Value.Select(static i => i.HasValue ? i.Value.ToString() : null).Distinct(StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }
        return null;
    }

    public static bool? ConvertToBool(ValueStatement value)
    {
        if (value is StringValueStatement svs)
        {
            return Convert.ToBoolean(svs.Value);
        }
        else if (value is NumberValueStatement nvs)
        {
            return Convert.ToBoolean(nvs.Value);
        }
        else if (value is BooleanValueStatement bvs)
        {
            return bvs.Value;
        }
        else if (value is NullValueStatement)
        {
            return null;
        }

        return null;
    }

    public static FrozenSet<bool> ConvertToBool(ArrayValueStatement value)
    {
        if (value is StringArrayValueStatement svs && svs.Value != null)
        {
            return svs.Value.Where(static i => i != null).Select(static i => Convert.ToBoolean(i)).Distinct().ToFrozenSet();
        }
        else if (value is BooleanArrayValueStatement b && b.Value != null)
        {
            return b.Value.Where(static i => i.HasValue).Select(static i => i.Value).Distinct().ToFrozenSet();
        }
        else if (value is NumberArrayValueStatement n && n.Value != null)
        {
            return n.Value.Where(static i => i.HasValue).Select(static i => i.Value).Select(static i => Convert.ToBoolean(i)).Distinct().ToFrozenSet();
        }
        return null;
    }

    public static long? ConvertToInt64(ValueStatement value)
    {
        if (value is StringValueStatement svs)
        {
            return Convert.ToInt64(svs.Value);
        }
        else if (value is NumberValueStatement nvs)
        {
            return Convert.ToInt64(nvs.Value);
        }
        else if (value is BooleanValueStatement bvs)
        {
            return Convert.ToInt64(bvs.Value);
        }
        else if (value is NullValueStatement)
        {
            return null;
        }

        return null;
    }

    public static FrozenSet<long> ConvertToInt64(ArrayValueStatement value)
    {
        if (value is StringArrayValueStatement svs && svs.Value != null)
        {
            return svs.Value.Where(static i => i != null).Select(static i => Convert.ToInt64(i)).Distinct().ToFrozenSet();
        }
        else if (value is BooleanArrayValueStatement b && b.Value != null)
        {
            return b.Value.Where(static i => i.HasValue).Select(static i => i.Value).Select(static i => Convert.ToInt64(i)).Distinct().ToFrozenSet();
        }
        else if (value is NumberArrayValueStatement n && n.Value != null)
        {
            return n.Value.Where(static i => i.HasValue).Select(static i => i.Value).Select(static i => Convert.ToInt64(i)).Distinct().ToFrozenSet();
        }
        return null;
    }

    public static bool AnyNull(ArrayValueStatement value)
    {
        if (value is StringArrayValueStatement svs && svs.Value != null)
        {
            return svs.Value.Any(static i => i == null);
        }
        else if (value is BooleanArrayValueStatement b && b.Value != null)
        {
            return b.Value.Any(static i => !i.HasValue);
        }
        else if (value is NumberArrayValueStatement n && n.Value != null)
        {
            return n.Value.Any(static i => !i.HasValue);
        }
        return false;
    }
}
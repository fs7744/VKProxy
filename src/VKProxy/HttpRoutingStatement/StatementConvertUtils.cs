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
            return b.Value.Distinct().Select(static i => i.ToString()).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }
        else if (value is NumberArrayValueStatement n && n.Value != null)
        {
            return n.Value.Distinct().Select(static i => i.ToString()).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
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

        return null;
    }

    public static FrozenSet<bool> ConvertToBool(ArrayValueStatement value)
    {
        if (value is StringArrayValueStatement svs && svs.Value != null)
        {
            return svs.Value.Select(static i => Convert.ToBoolean(i)).Distinct().ToFrozenSet();
        }
        else if (value is BooleanArrayValueStatement b && b.Value != null)
        {
            return b.Value.Distinct().ToFrozenSet();
        }
        else if (value is NumberArrayValueStatement n && n.Value != null)
        {
            return n.Value.Select(static i => Convert.ToBoolean(i)).Distinct().ToFrozenSet();
        }
        return null;
    }
}
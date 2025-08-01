using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using System.Text;
using VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement;

public static partial class HttpRoutingStatementParser
{
    public static string ConvertToString(this Statement statement)
    {
        var sb = new StringBuilder();
        ConvertToString(sb, statement);
        return sb.ToString();
    }

    public static void ConvertToString(StringBuilder sb, Statement statement)
    {
        if (statement is OperaterStatement os)
        {
            DoConvertToString(sb, os);
        }
        else if (statement is UnaryOperaterStatement uo)
        {
            DoConvertToString(sb, uo);
        }
        else if (statement is InOperaterStatement io)
        {
            DoConvertToString(sb, io);
        }
        else if (statement is ConditionsStatement conditions)
        {
            if (conditions.Condition == Condition.And)
            {
                sb.Append(" (");
                ConvertToString(sb, conditions.Left);
                sb.Append(" and ");
                ConvertToString(sb, conditions.Right);
                sb.Append(") ");
            }
            else
            {
                sb.Append(" (");
                ConvertToString(sb, conditions.Left);
                sb.Append(" or ");
                ConvertToString(sb, conditions.Right);
                sb.Append(") ");
            }
        }
    }

    internal static Func<HttpContext, string> ConvertToStringFunc(ValueStatement statement)
    {
        if (statement is StringValueStatement s)
        {
            var vv = s.Value;
            return c => vv.ToString();
        }
        else if (statement is BooleanValueStatement b)
        {
            var vv = b.Value;
            return c => vv.ToString();
        }
        else if (statement is NumberValueStatement n)
        {
            var vv = n.Value;
            return c => vv.ToString();
        }
        else if (statement is NullValueStatement)
        {
            return c => null;
        }
        else if (statement is ArrayValueStatement value)
        {
            string ss = null;
            if (value is StringArrayValueStatement svs && svs.Value != null)
            {
                ss = string.Join(",", svs.Value);
                return c => ss;
            }
            else if (value is BooleanArrayValueStatement bb && bb.Value != null)
            {
                ss = string.Join(",", bb.Value);
                return c => ss;
            }
            else if (value is NumberArrayValueStatement nn && nn.Value != null)
            {
                ss = string.Join(",", nn.Value);
                return c => ss;
            }
            return null;
        }
        else if (statement is FieldStatement f)
        {
            if (f is DynamicFieldStatement d && !string.IsNullOrWhiteSpace(d.Key))
            {
                if (fieldConverters.TryGetValue($"{d.Field}{d.Key}", out var converter) && converter is IStaticFieldStatementFuncConverter c)
                {
                    return c.ConvertToString();
                }
                else if (fieldConverters.TryGetValue(d.Field, out converter) && converter is IDynamicFieldStatementFuncConverter dc)
                {
                    return dc.ConvertToString(d.Key);
                }
            }
            else if (fieldConverters.TryGetValue(f.Field, out var converter) && converter is IStaticFieldStatementFuncConverter c)
            {
                return c.ConvertToString();
            }
        }

        return null;
    }

    private static void DoConvertToString(StringBuilder sb, InOperaterStatement io)
    {
        sb.Append(' ');
        DoConvertToString(sb, io.Left);
        sb.Append(" in (");
        DoConvertToString(sb, io.Right);
        sb.Append(") ");
    }

    private static void DoConvertToString(StringBuilder sb, ArrayValueStatement array)
    {
        if (array is StringArrayValueStatement s)
        {
            for (var i = 0; i < s.Value.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');

                var bb = s.Value[i];
                if (bb == null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append("'");
                    sb.Append(bb.Replace("'", "\\'"));
                    sb.Append("'");
                }
            }
        }
        else if (array is BooleanArrayValueStatement b)
        {
            for (var i = 0; i < b.Value.Count; i++)
            {
                var bb = b.Value[i];
                if (i > 0)
                    sb.Append(',');
                sb.Append(bb.HasValue ? (bb.Value ? "true" : "false") : "null");
            }
        }
        else if (array is NumberArrayValueStatement n)
        {
            for (var i = 0; i < n.Value.Count; i++)
            {
                var bb = n.Value[i];
                if (i > 0)
                    sb.Append(',');
                sb.Append(bb.HasValue ? bb : "null");
            }
        }
    }

    private static void DoConvertToString(StringBuilder sb, UnaryOperaterStatement uo)
    {
        if (uo.Operater == "not")
        {
            sb.Append(" not (");
            ConvertToString(sb, uo.Right);
            sb.Append(") ");
        }
    }

    private static void DoConvertToString(StringBuilder sb, OperaterStatement os)
    {
        sb.Append(' ');
        DoConvertToString(sb, os.Left);
        sb.Append(' ');
        sb.Append(os.Operater);
        sb.Append(' ');
        DoConvertToString(sb, os.Right);
        sb.Append(' ');
    }

    private static void DoConvertToString(StringBuilder sb, ValueStatement v)
    {
        if (v is DynamicFieldStatement d)
        {
            sb.Append(d.Field);
            sb.Append("(");
            sb.Append("'");
            sb.Append(d.Key.Replace("'", "\\'"));
            sb.Append("'");
            sb.Append(")");
        }
        else if (v is FieldStatement f)
        {
            sb.Append(f.Field);
        }
        else if (v is StringValueStatement s)
        {
            sb.Append("'");
            sb.Append(s.Value.Replace("'", "\\'"));
            sb.Append("'");
        }
        else if (v is BooleanValueStatement b)
        {
            sb.Append(b.Value ? "true" : "false");
        }
        else if (v is NumberValueStatement n)
        {
            sb.Append(n.Value.ToString());
        }
        else if (v is NullValueStatement)
        {
            sb.Append("null");
        }
    }
}
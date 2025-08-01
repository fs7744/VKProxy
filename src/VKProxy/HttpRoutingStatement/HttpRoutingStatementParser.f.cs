using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement;

public static partial class HttpRoutingStatementParser
{
    private static readonly FrozenDictionary<string, IFieldStatementFuncConverter> fieldConverters = new IFieldStatementFuncConverter[]
    {
        new PathFuncConverter(),
        new MethodFuncConverter(),
        new SchemeFuncConverter(),
        new ContentTypeFuncConverter(),
        new HostFuncConverter(),
        new QueryStringFuncConverter(),
        new ProtocolFuncConverter(),
    }.ToFrozenDictionary(i => i.Field, StringComparer.OrdinalIgnoreCase);

    public static Func<HttpContext, bool> ConvertToFunction(string statement)
    {
        var statements = ParseStatements(statement);
        if (statements.Count > 1)
        {
            throw new ParserExecption($"statements must be only one");
        }
        var f = ConvertToFunction(statements.Pop());
        if (f == null)
        {
            throw new ParserExecption($"Can't parse {statement}");
        }
        return f;
    }

    public static Func<HttpContext, bool> ConvertToFunction(Statement statement)
    {
        if (statement is OperaterStatement os)
        {
            return DoConvertToFunction(os);
        }
        else if (statement is UnaryOperaterStatement uo)
        {
            return DoConvertToFunction(uo);
        }
        else if (statement is InOperaterStatement io)
        {
            return DoConvertToFunction(io);
        }
        else if (statement is ConditionsStatement conditions)
        {
            if (conditions.Condition == Condition.And)
            {
                var l = ConvertToFunction(conditions.Left);
                var r = ConvertToFunction(conditions.Right);
                if (l != null && r != null)
                {
                    return c => l(c) && r(c);
                }
            }
            else
            {
                var l = ConvertToFunction(conditions.Left);
                var r = ConvertToFunction(conditions.Right);
                if (l != null && r != null)
                {
                    return c => l(c) || r(c);
                }
            }
        }

        return null;
    }

    private static Func<HttpContext, bool> DoConvertToFunction(OperaterStatement os)
    {
        if (os.Left is FieldStatement field && os.Right is not FieldStatement)
        {
            return DoConvertToFunction(field, os.Right, os.Operater);
        }
        else if (os.Right is FieldStatement f && os.Left is not FieldStatement)
        {
            return DoConvertToFunction(f, os.Left, Negate(os.Operater));
        }
        return null;
    }

    private static string Negate(string operater)
    {
        return operater switch
        {
            "<" => ">=",
            "<=" => ">",
            ">" => "<=",
            ">=" => "<",
            _ => operater,
        };
    }

    private static Func<HttpContext, bool> DoConvertToFunction(FieldStatement field, ValueStatement value, string operater)
    {
        if (field is DynamicFieldStatement d && !string.IsNullOrWhiteSpace(d.Key))
        {
            if (fieldConverters.TryGetValue($"{d.Field}{d.Key}", out var converter))
            {
                return converter.Convert(value, operater);
            }
            //else if ()
            //{
            //}
        }
        else if (fieldConverters.TryGetValue(field.Field, out var converter))
        {
            return converter.Convert(value, operater);
        }

        return null;
    }

    private static Func<HttpContext, bool> DoConvertToFunction(UnaryOperaterStatement uo)
    {
        if (uo.Operater.Equals("not", StringComparison.OrdinalIgnoreCase))
        {
            var o = ConvertToFunction(uo.Right);
            return o == null ? null : c => !o(c);
        }
        return null;
    }

    private static Func<HttpContext, bool> DoConvertToFunction(InOperaterStatement io)
    {
        if (io.Left is FieldStatement field)
        {
            return DoConvertToFunction(field, io.Right, io.Operater);
        }
        return null;
    }
}
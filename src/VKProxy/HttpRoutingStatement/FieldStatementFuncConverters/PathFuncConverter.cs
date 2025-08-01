using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class PathFuncConverter : IFieldStatementFuncConverter
{
    public virtual string Field => "Path";

    public Func<HttpContext, bool> Convert(ValueStatement value, string operater)
    {
        switch (operater)
        {
            case "=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    return CreateEqualsFunc(str);
                }
            case "!=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    return CreateNotEqualsFunc(str);
                }
            case "~=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    if (string.IsNullOrWhiteSpace(str)) return null;
                    var reg = new Regex(str, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return CreateRegexFunc(reg);
                }
            case "in":
                if (value is ArrayValueStatement avs)
                {
                    var set = StatementConvertUtils.ConvertToString(avs);
                    if (set == null) return null;
                    return CreateSetContainsFunc(set);
                }
                else
                    return null;

            default:
                return null;
        }
    }

    protected virtual Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return set.Contains(path);
        };
    }

    protected virtual Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return reg.IsMatch(path);
        };
    }

    protected virtual Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return !string.Equals(path, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected virtual Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return string.Equals(path, str, StringComparison.OrdinalIgnoreCase);
        };
    }
}
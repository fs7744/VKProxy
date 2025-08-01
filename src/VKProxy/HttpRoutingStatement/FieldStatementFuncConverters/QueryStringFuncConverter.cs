using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class QueryStringFuncConverter : StringFuncConverter
{
    public override string Field => "QueryString";

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var v = c.Request.QueryString.Value;
            return v != null && reg.IsMatch(v);
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var v = c.Request.QueryString.Value;
            return !string.Equals(v, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var v = c.Request.QueryString.Value;
            return string.Equals(v, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var v = c.Request.QueryString.Value;
            return v != null && set.Contains(v);
        };
    }
}
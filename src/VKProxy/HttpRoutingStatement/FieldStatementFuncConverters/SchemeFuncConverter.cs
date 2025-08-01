using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class SchemeFuncConverter : StringFuncConverter
{
    public override string Field => "Scheme";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c => c.Request.Scheme;
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var v = c.Request.Scheme;
            return reg.IsMatch(v);
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var v = c.Request.Scheme;
            return !string.Equals(v, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var v = c.Request.Scheme;
            return string.Equals(v, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var v = c.Request.Scheme;
            return set.Contains(v);
        };
    }
}
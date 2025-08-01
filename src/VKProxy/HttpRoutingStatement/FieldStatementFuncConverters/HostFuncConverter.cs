using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class HostFuncConverter : StringFuncConverter
{
    public override string Field => "Host";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c => c.Request.Host.ToString();
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var v = c.Request.Host.ToString();
            return reg.IsMatch(v);
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var v = c.Request.Host.ToString();
            return !string.Equals(v, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var v = c.Request.Host.ToString();
            return string.Equals(v, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var v = c.Request.Host.ToString();
            return set.Contains(v);
        };
    }
}
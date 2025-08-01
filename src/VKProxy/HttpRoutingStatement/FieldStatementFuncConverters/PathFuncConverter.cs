using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class PathFuncConverter : StringFuncConverter
{
    public override string Field => "Path";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c => c.Request.Path.Value;
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return set.Contains(path);
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return reg.IsMatch(path);
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return !string.Equals(path, str, StringComparison.OrdinalIgnoreCase);
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var path = c.Request.Path.Value;
            return string.Equals(path, str, StringComparison.OrdinalIgnoreCase);
        };
    }
}
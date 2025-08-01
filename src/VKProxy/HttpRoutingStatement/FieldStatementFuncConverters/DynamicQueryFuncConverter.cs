using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class DynamicQueryFuncConverter : DynamicStringFuncConverter
{
    public override string Field => "Query";

    public override Func<HttpContext, string> ConvertToString(string key)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return null;
            if (h.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string key, string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null || h.Count == 0) return false;
            if (h.TryGetValue(key, out var values))
            {
                foreach (var value in values)
                {
                    if (string.Equals(value, str, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string key, string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null || h.Count == 0) return false;
            if (h.TryGetValue(key, out var values))
            {
                foreach (var value in values)
                {
                    if (string.Equals(value, str, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }
            return true;
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(string key, Regex reg)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null || h.Count == 0) return false;
            if (h.TryGetValue(key, out var values))
            {
                foreach (var value in values)
                {
                    if (reg.IsMatch(value))
                    {
                        return true;
                    }
                }
            }
            return false;
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(string key, FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null || h.Count == 0) return false;
            if (h.TryGetValue(key, out var values))
            {
                foreach (var value in values)
                {
                    if (set.Contains(value))
                    {
                        return true;
                    }
                }
            }
            return false;
        };
    }
}
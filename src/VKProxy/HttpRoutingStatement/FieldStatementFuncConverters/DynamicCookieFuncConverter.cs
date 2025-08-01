using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class DynamicCookieFuncConverter : DynamicStringFuncConverter
{
    public override string Field => "Cookie";

    public override Func<HttpContext, string> ConvertToString(string key)
    {
        return c =>
        {
            var h = c.Request.Cookies;
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
            var h = c.Request.Cookies;
            if (h == null) return false;
            if (h.TryGetValue(key, out var value))
            {
                if (string.Equals(value, str, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string key, string str)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            if (h.TryGetValue(key, out var value))
            {
                if (string.Equals(value, str, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(string key, Regex reg)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            if (h.TryGetValue(key, out var value))
            {
                if (reg.IsMatch(value))
                {
                    return true;
                }
            }
            return false;
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(string key, FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            if (h.TryGetValue(key, out var value))
            {
                if (set.Contains(value))
                {
                    return true;
                }
                return false;
            }
            return false;
        };
    }
}
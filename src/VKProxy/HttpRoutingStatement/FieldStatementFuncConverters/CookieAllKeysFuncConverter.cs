using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class CookieAllKeysFuncConverter : StringFuncConverter
{
    public override string Field => "Cookie#Keys";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return null;
            return string.Join(',', h.Keys);
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => reg.IsMatch(i.Key));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return !h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => set.Contains(i.Key));
        };
    }
}

internal class CookieAllValuesFuncConverter : StringFuncConverter
{
    public override string Field => "Cookie#Values";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return null;
            return string.Join(',', h.Select(static i => i.Value));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => reg.IsMatch(i.Value));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return !h.Any(i => string.Equals(i.Value, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => string.Equals(i.Value, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => set.Contains(i.Value));
        };
    }
}

internal class CookieAllKVSFuncConverter : StringFuncConverter
{
    public override string Field => "Cookie#KVS";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return null;
            return string.Join(',', h.Select(static i => i.ToString()));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => reg.IsMatch(i.Key) || reg.IsMatch(i.Value));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return !h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Value, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Value, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Cookies;
            if (h == null) return false;
            return h.Any(i => set.Contains(i.Key) || set.Contains(i.Value));
        };
    }
}
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class HeaderAllKeysFuncConverter : StringFuncConverter
{
    public override string Field => "Header#Keys";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Headers;
            if (h == null || h.Count == 0) return null;
            return string.Join(',', h.Keys);
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => reg.IsMatch(i.Key));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return !headers.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => set.Contains(i.Key));
        };
    }
}

internal class HeaderAllValuesFuncConverter : StringFuncConverter
{
    public override string Field => "Header#Values";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Headers;
            if (h == null || h.Count == 0) return null;
            return string.Join(',', h.Select(static i => i.Value));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => i.Value.Any(reg.IsMatch));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return !headers.Any(i => i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => i.Value.Any(j => set.Contains(j)));
        };
    }
}

internal class HeaderAllKVSFuncConverter : StringFuncConverter
{
    public override string Field => "Header#KVS";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Headers;
            if (h == null || h.Count == 0) return null;
            return string.Join(',', h.Select(static i => i.ToString()));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => reg.IsMatch(i.Key) || i.Value.Any(reg.IsMatch));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return !headers.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var headers = c.Request.Headers;
            if (headers == null || headers.Count == 0) return false;
            return headers.Any(i => set.Contains(i.Key) || i.Value.Any(j => set.Contains(j)));
        };
    }
}
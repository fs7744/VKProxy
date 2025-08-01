using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class QueryAllKeysFuncConverter : StringFuncConverter
{
    public override string Field => "Query#Keys";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Query;
            if (h == null) return null;
            return string.Join(',', h.Keys);
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => reg.IsMatch(i.Key));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return !h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => set.Contains(i.Key));
        };
    }
}

internal class QueryAllValuesFuncConverter : StringFuncConverter
{
    public override string Field => "Query#Values";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Query;
            if (h == null) return null;
            return string.Join(',', h.Select(static i => i.Value));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => i.Value.Any(reg.IsMatch));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return !h.Any(i => i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => i.Value.Any(j => set.Contains(j)));
        };
    }
}

internal class QueryAllKVSFuncConverter : StringFuncConverter
{
    public override string Field => "Query#KVS";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            var h = c.Request.Query;
            if (h == null) return null;
            return string.Join(',', h.Select(static i => i.ToString()));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => reg.IsMatch(i.Key) || i.Value.Any(reg.IsMatch));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return !h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            var h = c.Request.Query;
            if (h == null) return false;
            return h.Any(i => set.Contains(i.Key) || i.Value.Any(j => set.Contains(j)));
        };
    }
}
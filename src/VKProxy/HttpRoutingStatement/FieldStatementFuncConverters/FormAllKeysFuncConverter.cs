using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class FormAllKeysFuncConverter : StringFuncConverter
{
    public override string Field => "Form#Keys";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            if (!c.Request.HasFormContentType) return null;
            var h = c.Request.Form;
            return string.Join(',', h.Keys);
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => reg.IsMatch(i.Key));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return !h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => set.Contains(i.Key));
        };
    }
}

internal class FormAllValuesFuncConverter : StringFuncConverter
{
    public override string Field => "Form#Values";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            if (!c.Request.HasFormContentType) return null;
            var h = c.Request.Form;
            return string.Join(',', h.Select(static i => i.Value));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => i.Value.Any(reg.IsMatch));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return !h.Any(i => i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => i.Value.Any(j => set.Contains(j)));
        };
    }
}

internal class FormAllKVSFuncConverter : StringFuncConverter
{
    public override string Field => "Form#KVS";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c =>
        {
            if (!c.Request.HasFormContentType) return null;
            var h = c.Request.Form;
            return string.Join(',', h.Select(static i => i.ToString()));
        };
    }

    protected override Func<HttpContext, bool> CreateRegexFunc(Regex reg)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => reg.IsMatch(i.Key) || i.Value.Any(reg.IsMatch));
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(string str)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return !h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(string str)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => string.Equals(i.Key, str, StringComparison.OrdinalIgnoreCase) || i.Value.Any(j => string.Equals(j, str, StringComparison.OrdinalIgnoreCase)));
        };
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
            if (h == null || h.Count == 0) return false;
            return h.Any(i => set.Contains(i.Key) || i.Value.Any(j => set.Contains(j)));
        };
    }
}
using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class DynamicFormFuncConverter : DynamicStringFuncConverter
{
    public override string Field => "Form";

    public override Func<HttpContext, string> ConvertToString(string key)
    {
        return c =>
        {
            if (!c.Request.HasFormContentType) return null;
            var h = c.Request.Form;
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
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
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
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
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
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
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
            if (!c.Request.HasFormContentType) return false;
            var h = c.Request.Form;
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
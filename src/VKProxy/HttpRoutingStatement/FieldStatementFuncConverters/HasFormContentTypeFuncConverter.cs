using Microsoft.AspNetCore.Http;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class HasFormContentTypeFuncConverter : BoolFuncConverter
{
    public override string Field => "HasFormContentType";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c => c.Request.HasFormContentType.ToString();
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<bool> set)
    {
        return c =>
        {
            var v = c.Request.HasFormContentType;
            return set.Contains(v);
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(bool vv)
    {
        return c =>
        {
            return c.Request.HasFormContentType != vv;
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(bool vv)
    {
        return c =>
        {
            return c.Request.HasFormContentType == vv;
        };
    }
}
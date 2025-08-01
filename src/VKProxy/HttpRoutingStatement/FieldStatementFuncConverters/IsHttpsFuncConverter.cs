using Microsoft.AspNetCore.Http;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class IsHttpsFuncConverter : BoolFuncConverter
{
    public override string Field => "IsHttps";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c => c.Request.IsHttps.ToString();
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<bool> set)
    {
        return c =>
        {
            var v = c.Request.IsHttps;
            return set.Contains(v);
        };
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(bool vv)
    {
        return c =>
        {
            return c.Request.IsHttps != vv;
        };
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(bool vv)
    {
        return c =>
        {
            return c.Request.IsHttps == vv;
        };
    }
}
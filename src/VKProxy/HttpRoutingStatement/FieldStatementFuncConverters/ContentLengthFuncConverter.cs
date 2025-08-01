using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class ContentLengthFuncConverter : LongFuncConverter
{
    public override string Field => "ContentLength";

    public override Func<HttpContext, string> ConvertToString()
    {
        return static c => c.Request.ContentLength?.ToString();
    }

    protected override Func<HttpContext, bool> CreateEqualsFunc(long vv)
    {
        return c => c.Request.ContentLength == vv;
    }

    protected override Func<HttpContext, bool> CreateEqualsNullFunc()
    {
        return static c => !c.Request.ContentLength.HasValue;
    }

    protected override Func<HttpContext, bool> CreateGreaterThanFunc(long value)
    {
        return c => c.Request.ContentLength > value;
    }

    protected override Func<HttpContext, bool> CreateGreaterThanOrEqualFunc(long value)
    {
        return c => c.Request.ContentLength >= value;
    }

    protected override Func<HttpContext, bool> CreateLessThanFunc(long value)
    {
        return c => c.Request.ContentLength < value;
    }

    protected override Func<HttpContext, bool> CreateLessThanOrEqualFunc(long value)
    {
        return c => c.Request.ContentLength <= value;
    }

    protected override Func<HttpContext, bool> CreateNotEqualsFunc(long vv)
    {
        return c => c.Request.ContentLength != vv;
    }

    protected override Func<HttpContext, bool> CreateNotEqualsNullFunc()
    {
        return static c => c.Request.ContentLength.HasValue;
    }

    protected override Func<HttpContext, bool> CreateSetContainsFunc(FrozenSet<long> set, bool allowNull)
    {
        return allowNull
            ? c => !c.Request.ContentLength.HasValue || set.Contains(c.Request.ContentLength.Value)
            : c => c.Request.ContentLength.HasValue && set.Contains(c.Request.ContentLength.Value);
    }
}
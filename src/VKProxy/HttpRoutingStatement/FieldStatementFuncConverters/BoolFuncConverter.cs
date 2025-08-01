using Microsoft.AspNetCore.Http;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal abstract class BoolFuncConverter : IStaticFieldStatementFuncConverter
{
    public abstract string Field { get; }

    public Func<HttpContext, bool> Convert(ValueStatement value, string operater)
    {
        switch (operater)
        {
            case "=":
                {
                    var v = StatementConvertUtils.ConvertToBool(value);
                    if (!v.HasValue) return null;
                    return CreateEqualsFunc(v.Value);
                }
            case "!=":
                {
                    var v = StatementConvertUtils.ConvertToBool(value);
                    if (v.HasValue) return null;
                    return CreateNotEqualsFunc(v.Value);
                }
            case "in":
                if (value is ArrayValueStatement avs)
                {
                    var set = StatementConvertUtils.ConvertToBool(avs);
                    if (set == null) return null;
                    return CreateSetContainsFunc(set);
                }
                else
                    return null;

            default:
                return null;
        }
    }

    protected abstract Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<bool> set);

    protected abstract Func<HttpContext, bool> CreateNotEqualsFunc(bool vv);

    protected abstract Func<HttpContext, bool> CreateEqualsFunc(bool vv);
}
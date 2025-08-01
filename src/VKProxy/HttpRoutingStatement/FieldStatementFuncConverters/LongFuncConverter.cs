using Microsoft.AspNetCore.Http;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal abstract class LongFuncConverter : IStaticFieldStatementFuncConverter
{
    public abstract string Field { get; }

    public Func<HttpContext, bool> Convert(ValueStatement value, string operater)
    {
        switch (operater)
        {
            case "<":
                {
                    var v = StatementConvertUtils.ConvertToInt64(value);
                    if (!v.HasValue) return CreateEqualsNullFunc();
                    return CreateLessThanFunc(v.Value);
                }
            case "<=":
                {
                    var v = StatementConvertUtils.ConvertToInt64(value);
                    if (!v.HasValue) return CreateEqualsNullFunc();
                    return CreateLessThanOrEqualFunc(v.Value);
                }
            case ">":
                {
                    var v = StatementConvertUtils.ConvertToInt64(value);
                    if (!v.HasValue) return CreateEqualsNullFunc();
                    return CreateGreaterThanFunc(v.Value);
                }
            case ">=":
                {
                    var v = StatementConvertUtils.ConvertToInt64(value);
                    if (!v.HasValue) return CreateEqualsNullFunc();
                    return CreateGreaterThanOrEqualFunc(v.Value);
                }
            case "=":
                {
                    var v = StatementConvertUtils.ConvertToInt64(value);
                    if (!v.HasValue) return CreateEqualsNullFunc();
                    return CreateEqualsFunc(v.Value);
                }
            case "!=":
                {
                    var v = StatementConvertUtils.ConvertToInt64(value);
                    if (!v.HasValue) return CreateNotEqualsNullFunc();
                    return CreateNotEqualsFunc(v.Value);
                }
            case "in":
                if (value is ArrayValueStatement avs)
                {
                    var set = StatementConvertUtils.ConvertToInt64(avs);
                    if (set == null) return null;
                    return CreateSetContainsFunc(set, StatementConvertUtils.AnyNull(avs));
                }
                else
                    return null;

            default:
                return null;
        }
    }

    protected abstract Func<HttpContext, bool> CreateGreaterThanOrEqualFunc(long value);

    protected abstract Func<HttpContext, bool> CreateGreaterThanFunc(long value);

    protected abstract Func<HttpContext, bool> CreateLessThanOrEqualFunc(long value);

    protected abstract Func<HttpContext, bool> CreateLessThanFunc(long value);

    protected abstract Func<HttpContext, bool> CreateEqualsNullFunc();

    protected abstract Func<HttpContext, bool> CreateNotEqualsNullFunc();

    protected abstract Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<long> set, bool allowNull);

    protected abstract Func<HttpContext, bool> CreateNotEqualsFunc(long vv);

    protected abstract Func<HttpContext, bool> CreateEqualsFunc(long vv);

    public abstract Func<HttpContext, string> ConvertToString();
}
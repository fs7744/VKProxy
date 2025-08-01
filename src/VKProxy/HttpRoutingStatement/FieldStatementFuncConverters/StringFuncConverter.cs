using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal abstract class StringFuncConverter : IStaticFieldStatementFuncConverter
{
    public abstract string Field { get; }

    public Func<HttpContext, bool> Convert(ValueStatement value, string operater)
    {
        switch (operater)
        {
            case "=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    return CreateEqualsFunc(str);
                }
            case "!=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    return CreateNotEqualsFunc(str);
                }
            case "~=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    if (string.IsNullOrWhiteSpace(str)) return null;
                    var reg = new Regex(str, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return CreateRegexFunc(reg);
                }
            case "in":
                if (value is ArrayValueStatement avs)
                {
                    var set = StatementConvertUtils.ConvertToString(avs);
                    if (set == null) return null;
                    return CreateSetContainsFunc(set);
                }
                else
                    return null;

            default:
                return null;
        }
    }

    protected abstract Func<HttpContext, bool> CreateSetContainsFunc(System.Collections.Frozen.FrozenSet<string> set);

    protected abstract Func<HttpContext, bool> CreateRegexFunc(Regex reg);

    protected abstract Func<HttpContext, bool> CreateNotEqualsFunc(string str);

    protected abstract Func<HttpContext, bool> CreateEqualsFunc(string str);
}
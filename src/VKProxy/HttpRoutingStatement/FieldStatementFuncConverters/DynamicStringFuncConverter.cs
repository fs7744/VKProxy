using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal abstract class DynamicStringFuncConverter : IDynamicFieldStatementFuncConverter
{
    public abstract string Field { get; }

    public Func<HttpContext, bool> Convert(ValueStatement value, string operater, string key)
    {
        switch (operater)
        {
            case "=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    return CreateEqualsFunc(key, str);
                }
            case "!=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    return CreateNotEqualsFunc(key, str);
                }
            case "~=":
                {
                    var str = StatementConvertUtils.ConvertToString(value);
                    if (string.IsNullOrWhiteSpace(str)) return null;
                    var reg = new Regex(str, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return CreateRegexFunc(key, reg);
                }
            case "in":
                if (value is ArrayValueStatement avs)
                {
                    var set = StatementConvertUtils.ConvertToString(avs);
                    if (set == null) return null;
                    return CreateSetContainsFunc(key, set);
                }
                else
                    return null;

            default:
                return null;
        }
    }

    protected abstract Func<HttpContext, bool> CreateSetContainsFunc(string key, System.Collections.Frozen.FrozenSet<string> set);

    protected abstract Func<HttpContext, bool> CreateRegexFunc(string key, Regex reg);

    protected abstract Func<HttpContext, bool> CreateNotEqualsFunc(string key, string str);

    protected abstract Func<HttpContext, bool> CreateEqualsFunc(string key, string str);

    public abstract Func<HttpContext, string> ConvertToString(string key);
}
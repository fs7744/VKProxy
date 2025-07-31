using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement.FieldStatementFuncConverters;

internal class PathFuncConverter : IFieldStatementFuncConverter
{
    public string Field => "Path";

    public Func<HttpContext, bool> Convert(ValueStatement value, string operater)
    {
        switch (operater)
        {
            case "=":
                {
                    var str = ConvertToString(value);
                    return c =>
                    {
                        var path = c.Request.Path.Value;
                        return string.Equals(path, str, StringComparison.OrdinalIgnoreCase);
                    };
                }
            case "!=":
                {
                    var str = ConvertToString(value);
                    return c =>
                    {
                        var path = c.Request.Path.Value;
                        return !string.Equals(path, str, StringComparison.OrdinalIgnoreCase);
                    };
                }
            case "~=":
                {
                    var str = ConvertToString(value);
                    if (string.IsNullOrWhiteSpace(str)) return null;
                    var reg = new Regex(str, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return c =>
                    {
                        var path = c.Request.Path.Value;
                        return reg.IsMatch(path);
                    };
                }
            default:
                return null;
        }
    }

    private static string ConvertToString(ValueStatement value)
    {
        if (value is StringValueStatement svs)
        {
            return svs.Value;
        }
        else if (value is NumberValueStatement nvs)
        {
            return nvs.Value.ToString();
        }
        else if (value is BooleanValueStatement bvs)
        {
            return bvs.Value.ToString();
        }
        else
        {
            throw new NotSupportedException($"Path Unsupported value type: {value.GetType().Name}");
        }
    }
}
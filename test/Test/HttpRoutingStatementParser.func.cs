﻿using Microsoft.AspNetCore.Http;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement;

public static partial class StatementParser
{
    public static Func<HttpContext, bool> ConvertToFunc(string statement)
    {
        var statements = HttpRoutingStatementParser.ParseStatements(statement);
        if (statements.Count > 1)
        {
            throw new ParserExecption($"statements must be only one");
        }
        var f = ConvertToFunc(statements.Pop());
        if (f == null)
        {
            throw new ParserExecption($"Can't parse {statement}");
        }
        return f;
    }

    private static readonly FrozenDictionary<string, Func<HttpContext, object>> fields = new Dictionary<string, Func<HttpContext, object>>()
    {
        { "Path", c => c.Request.Path.Value},
        { "Method", c => c.Request.Method},
        { "Scheme", c => c.Request.Scheme},
        { "IsHttps", c => c.Request.IsHttps},
        { "Protocol", c => c.Request.Protocol},
        { "ContentType", c => c.Request.ContentType},
        { "ContentLength", c => c.Request.ContentLength},
        { "HasFormContentType", c => c.Request.HasFormContentType},
        { "Host", c => c.Request.Host.ToString()},
        { "PathBase", c => c.Request.PathBase.ToString()},
        { "QueryString", c => c.Request.QueryString.ToString()}
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, Func<HttpContext, string, object>> dynamicFields = new Dictionary<string, Func<HttpContext, string, object>>()
    {
        { "Header", (c, k) => c.Request.Headers?[k].ToString()},
        { "Query", (c, k) => c.Request.Query?[k].ToString()},
        { "Cookie", (c, k) => c.Request.Cookies?[k]},
        { "Form", (c, k) => c.Request.HasFormContentType ? c.Request.Form?[k].ToString() : null},
        //{ "Route", (c, k) => c.Request.RouteValues?[k]}
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> dynamicKeys = new HashSet<string>() { "#Keys", "#Values", "#KVS" }.ToFrozenSet<string>(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, Func<HttpContext, IEnumerable<string>>> dynamicFieldEnumerables = new Dictionary<string, Func<HttpContext, IEnumerable<string>>>()
    {
        { "Header#Keys", (c) => c.Request.Headers?.Keys},
        { "Header#Values", (c) => c.Request.Headers?.Values?.SelectMany(i => i)},
        { "Header#KVS", (c) => c.Request.Headers == null ? null : c.Request.Headers.Keys.Union(c.Request.Headers.Values.SelectMany(i => i))},
        { "Query#Keys", (c) => c.Request.Query?.Keys},
        { "Query#Values", (c) => c.Request.Query?.SelectMany(i => i.Value)},
        { "Query#KVS", (c) => c.Request.Query == null ? null : c.Request.Query.Keys.Union(c.Request.Query.SelectMany(i => i.Value))},
        { "Cookie#Keys", (c) => c.Request.Cookies?.Keys},
        { "Cookie#Values", (c) => c.Request.Cookies?.Select(i => i.Value)},
        { "Cookie#KVS", (c) => c.Request.Cookies == null ? null : c.Request.Cookies.Keys.Union(c.Request.Cookies.Select(i => i.Value))},
        { "Form#Keys", (c) => c.Request.HasFormContentType && c.Request.Form != null ? c.Request.Form.Keys : null},
        { "Form#Values", (c) => c.Request.HasFormContentType && c.Request.Form != null ? c.Request.Form.SelectMany(i => i.Value) : null},
        { "Form#KVS", (c) => c.Request.HasFormContentType && c.Request.Form != null ? c.Request.Form.Keys.Union(c.Request.Form.SelectMany(i => i.Value)) : null},
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static Func<HttpContext, bool> ConvertToFunc(Statement statement)
    {
        if (statement is OperaterStatement os)
        {
            return DoConvertToFunc(os);
        }
        else if (statement is UnaryOperaterStatement uo)
        {
            return DoConvertToFunc(uo);
        }
        else if (statement is InOperaterStatement io)
        {
            return DoConvertToFunc(io);
        }
        else if (statement is ConditionsStatement conditions)
        {
            if (conditions.Condition == Condition.And)
            {
                var l = ConvertToFunc(conditions.Left);
                var r = ConvertToFunc(conditions.Right);
                if (l != null && r != null)
                {
                    return c => l(c) && r(c);
                }
            }
            else
            {
                var l = ConvertToFunc(conditions.Left);
                var r = ConvertToFunc(conditions.Right);
                if (l != null && r != null)
                {
                    return c => l(c) || r(c);
                }
            }
        }

        return null;
    }

    private static Func<HttpContext, bool> DoConvertToFunc(InOperaterStatement io)
    {
        if (io.Operater.Equals("in", StringComparison.OrdinalIgnoreCase))
        {
            var en = ConvertToFieldEnumerable(io.Left);
            var array = ConvertToArray(io.Right);
            if (array != null)
            {
                if (en != null)
                {
                    return c => en(c).Any(array);
                }
                else
                {
                    var o = ConvertToField(io.Left);
                    if (o != null)
                    {
                        return c => array(o(c));
                    }
                }
            }
        }
        return null;
    }

    private static Func<object, bool> ConvertToArray(ArrayValueStatement array)
    {
        if (array is StringArrayValueStatement s && s.Value != null)
        {
            var set = s.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            return o =>
            {
                if (o is null)
                    return false;
                else if (o is string str)
                    return set.Contains(str);
                else
                    return set.Contains(o.ToString());
            };
        }
        else if (array is BooleanArrayValueStatement b && b.Value != null)
        {
            var set = b.Value.Distinct().ToFrozenSet();
            return o =>
            {
                if (o is null)
                    return false;
                else
                {
                    var d = Convert.ToBoolean(o);
                    return set.Contains(d);
                }
            };
        }
        else if (array is NumberArrayValueStatement n && n.Value != null)
        {
            var set = n.Value.Distinct().ToFrozenSet();
            return o =>
            {
                if (o is null)
                    return false;
                else
                {
                    var d = Convert.ToDecimal(o);
                    return set.Contains(d);
                }
            };
        }

        return null;
    }

    internal static Func<HttpContext, IEnumerable<string>> ConvertToFieldEnumerable(ValueStatement v)
    {
        if (v is DynamicFieldStatement d)
        {
            if (!string.IsNullOrWhiteSpace(d.Key) && dynamicKeys.Contains(d.Key) && dynamicFields.ContainsKey(d.Field) && dynamicFieldEnumerables.TryGetValue($"{d.Field}{d.Key}", out var func))
            {
                return func;
            }
        }

        return null;
    }

    internal static Func<HttpContext, object> ConvertToField(ValueStatement v)
    {
        if (v is DynamicFieldStatement d)
        {
            if (!string.IsNullOrWhiteSpace(d.Key) && dynamicFields.TryGetValue(d.Field, out var func))
            {
                var k = d.Key;
                return c => func(c, k);
            }
            throw new ParserExecption($"Not support field {d.Field}('{d.Key}')");
        }
        else if (v is FieldStatement f)
        {
            if (fields.TryGetValue(f.Field, out var func))
                return func;
            throw new ParserExecption($"Not support field {f.Field}");
        }
        else if (v is StringValueStatement s)
        {
            var vv = s.Value;
            return c => vv;
        }
        else if (v is BooleanValueStatement b)
        {
            var vv = b.Value;
            return c => vv;
        }
        else if (v is NumberValueStatement n)
        {
            var vv = n.Value;
            return c => vv;
        }

        return null;
    }

    private static Func<HttpContext, bool> DoConvertToFunc(UnaryOperaterStatement uo)
    {
        if (uo.Operater.Equals("not", StringComparison.OrdinalIgnoreCase))
        {
            var o = ConvertToFunc(uo.Right);
            return o != null ? c => !o(c) : null;
        }
        return null;
    }

    private static Func<HttpContext, bool> DoConvertToFunc(OperaterStatement os)
    {
        var en = ConvertToFieldEnumerable(os.Left);
        if (en != null)
            return DoConvertToFunc(en, os, os.Right);
        en = ConvertToFieldEnumerable(os.Right);
        if (en != null)
            return DoConvertToFunc(en, os, os.Left);

        var l = ConvertToField(os.Left);
        var r = ConvertToField(os.Right);
        if (l == null || r == null) return null;
        switch (os.Operater)
        {
            case "<":
                return c =>
                {
                    var ld = l(c);
                    var rd = r(c);
                    if (ld == null || rd == null) return false;
                    return Convert.ToDecimal(ld) < Convert.ToDecimal(rd);
                };
            case "<=":
                return c =>
                {
                    var ld = l(c);
                    var rd = r(c);
                    if (ld == null || rd == null) return false;
                    return Convert.ToDecimal(ld) <= Convert.ToDecimal(rd);
                };
            case ">":
                return c =>
                {
                    var ld = l(c);
                    var rd = r(c);
                    if (ld == null || rd == null) return false;
                    return Convert.ToDecimal(ld) > Convert.ToDecimal(rd);
                };
            case ">=":
                return c =>
                {
                    var ld = l(c);
                    var rd = r(c);
                    if (ld == null || rd == null) return false;
                    return Convert.ToDecimal(ld) >= Convert.ToDecimal(rd);
                };
            case "=":
                return c =>
                {
                    var ld = l(c);
                    var rd = r(c);
                    if (ld is string s)
                    {
                        return string.Equals(s, rd?.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                    else if (ld is bool lb)
                    {
                        return lb == Convert.ToBoolean(rd);
                    }
                    else if (ld is long ll)
                    {
                        return ll == Convert.ToInt64(rd);
                    }
                    else if (ld is decimal ldd)
                    {
                        return ldd == Convert.ToDecimal(rd);
                    }
                    return ld == rd;
                };

            case "!=":
                return c =>
                {
                    var ld = l(c);
                    var rd = r(c);
                    if (ld is string s)
                    {
                        return !string.Equals(s, rd?.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                    else if (ld is bool lb)
                    {
                        return lb != Convert.ToBoolean(rd);
                    }
                    else if (ld is long ll)
                    {
                        return ll != Convert.ToInt64(rd);
                    }
                    else if (ld is decimal ldd)
                    {
                        return ldd != Convert.ToDecimal(rd);
                    }
                    return ld != rd;
                };

            case "~=":
                if (os.Left is FieldStatement && os.Right is StringValueStatement)
                {
                    var s = r(null)?.ToString();
                    var reg = string.IsNullOrWhiteSpace(s) ? null : new Regex(s, RegexOptions.Compiled);
                    if (reg != null)
                    {
                        return c =>
                        {
                            var d = l(c);
                            if (d == null) return false;
                            return reg.IsMatch(d.ToString());
                        };
                    }
                }
                return null;

            default:
                return null;
        }
    }

    private static Func<HttpContext, bool> DoConvertToFunc(Func<HttpContext, IEnumerable<string>> en, OperaterStatement os, ValueStatement v)
    {
        var r = ConvertToField(v);
        if (r == null) return null;
        switch (os.Operater)
        {
            case "=":
                return c =>
                {
                    var rd = r(c);
                    if (rd is string s)
                    {
                        return en(c).Any(ld => string.Equals(s, ld?.ToString(), StringComparison.OrdinalIgnoreCase));
                    }
                    return en(c).Any(ld => ld == rd);
                };
            case "!=":
                return c =>
                {
                    var rd = r(c);
                    if (rd is string s)
                    {
                        return en(c).Any(ld => !string.Equals(s, ld?.ToString(), StringComparison.OrdinalIgnoreCase));
                    }
                    return en(c).Any(ld => ld != rd);
                };
            case "~=":
                if (v is StringValueStatement)
                {
                    var s = r(null)?.ToString();
                    var reg = string.IsNullOrWhiteSpace(s) ? null : new Regex(s, RegexOptions.Compiled);
                    if (reg != null)
                    {
                        return c =>
                        {
                            return en(c).Any(reg.IsMatch);
                        };
                    }
                }
                return null;

            default:
                return null;
        }
    }
}
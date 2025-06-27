using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using System.Text;
using VKProxy.HttpRoutingStatement;
using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.TemplateStatement;

public interface ITemplateStatementFactory
{
    public Func<HttpContext, string> Convert(string template);
}

public class TemplateStatementFactory : ITemplateStatementFactory
{
    private readonly ObjectPool<StringBuilder> pool;
    private readonly ITokenParser[] tokenParsers;

    public TemplateStatementFactory(ObjectPoolProvider objectPoolProvider)
    {
        this.pool = objectPoolProvider.CreateStringBuilderPool();
        tokenParsers = new ITokenParser[] { new TemplateStatementSignTokenParser(), new TemplateStatementTokenParser() };
    }

    public Func<HttpContext, string> Convert(string template)
    {
        var tokens = Tokenize(template).ToList();
        if (tokens.Count == 0) return null;
        List<object> list = new List<object>();
        var enumerator = ((IEnumerable<Token>)tokens).GetEnumerator();
        while (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            if (c.Type == TokenType.Sign)
            {
                if ("{{".Equals(c.GetValue().ToString()) || "}}".Equals(c.GetValue().ToString()))
                {
                    list.Add(c.GetValue().ToString());
                    continue;
                }
                else if ("{".Equals(c.GetValue().ToString()))
                {
                    if (TryConvertField(enumerator, list))
                    {
                        continue;
                    }
                }

                throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
            }
            else
            {
                list.Add(c.GetValue().ToString());
            }
        }

        string last = null;
        List<Func<HttpContext, string>> funs = new();
        foreach (var t in list)
        {
            if (t is Func<HttpContext, string> f)
            {
                if (last != null)
                {
                    var l = last.ToUpperInvariant();
                    funs.Add(c => l);
                }
                last = null;
                funs.Add(f);
            }
            else
            {
                var s = t as string;
                if (last != null)
                {
                    last += s;
                }
                else
                {
                    last = s;
                }
            }
        }
        if (last != null)
        {
            var l = last.ToUpperInvariant();
            funs.Add(c => l);
        }

        return c =>
        {
            var sb = pool.Get();
            try
            {
                foreach (var t in funs)
                {
                    sb.Append(t(c));
                }

                return sb.ToString();
            }
            finally
            {
                pool.Return(sb);
            }
        };
    }

    private bool TryConvertField(IEnumerator<Token> enumerator, List<object> list)
    {
        if (enumerator.MoveNext())
        {
            var c = enumerator.Current;
            if (c.Type != TokenType.Sign)
            {
                var statements = HttpRoutingStatementParser.ParseStatements(c.GetValue().ToString());
                if (statements.Count == 1 && statements.First() is ValueStatement statement)
                {
                    var f = HttpRoutingStatementParser.ConvertToField(statement);
                    if (f != null)
                    {
                        Func<HttpContext, string> cc = c => f(c)?.ToString()?.ToUpperInvariant();
                        list.Add(cc);
                    }
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }

            if (enumerator.MoveNext() && enumerator.Current.GetValue().ToString() == "}")
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<Token> Tokenize(string statement)
    {
        var context = new TokenParserContext(statement);
        while (context.TryPeek(out var character))
        {
            bool matched = false;
            foreach (var parser in tokenParsers)
            {
                if (parser.TryTokenize(context, out var t))
                {
                    matched = true;
                    if (t != null)
                        yield return t;
                    break;
                }
            }
            if (!matched)
                throw new ParserExecption($"Can't parse near by {context.GetSomeChars()} (Line:{context.Line},Col:{context.Column})");
            if (!context.HasNext) { break; }
        }
    }
}
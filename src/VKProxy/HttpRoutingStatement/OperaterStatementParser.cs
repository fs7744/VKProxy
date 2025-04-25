using VKProxy.HttpRoutingStatement.Statements;

namespace VKProxy.HttpRoutingStatement;

public class OperaterStatementParser : IStatementParser
{
    public bool TryParse(StatementParserContext context)
    {
        if (context.HasToken())
        {
            var c = context.Current;
            if (c.Type == TokenType.Sign)
            {
                return ConvertSign(context, ref c);
            }
            else if (c.Type == TokenType.Word)
            {
                var v = c.GetValue();
                if (v.Equals("not", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.MoveNext())
                    {
                        var op = new UnaryOperaterStatement() { Operater = "not" };
                        var index = context.Index;
                        context.Stack.Push(op);
                        do
                        {
                            context.Parse(context, true);
                            if (op.Right != null)
                            {
                                return true;
                            }
                            if (context.Stack.TryPeek(out var vsss) && vsss != op && vsss is ConditionStatement vss)
                            {
                                context.Stack.Pop();
                                op.Right = vss;
                                if (context.Stack.Peek() == op)
                                {
                                    return true;
                                }
                            }
                        } while (context.HasToken());

                        context.Index = index;
                        c = context.Current;
                    }
                    throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
                }
                else if (v.Equals("or", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.MoveNext() && context.Stack.Peek() is ConditionStatement vs)
                    {
                        var op = new ConditionsStatement() { Condition = Condition.Or };
                        var index = context.Index;
                        op.Left = vs;
                        context.Stack.Pop();
                        context.Stack.Push(op);
                        do
                        {
                            context.Parse(context, true);
                            if (op.Right != null)
                            {
                                return true;
                            }
                            if (context.Stack.TryPeek(out var vsss) && vsss != op && vsss is ConditionStatement vss)
                            {
                                context.Stack.Pop();
                                op.Right = vss;
                                if (context.Stack.Peek() == op)
                                {
                                    return true;
                                }
                            }
                        } while (context.HasToken());

                        context.Index = index;
                        c = context.Current;
                    }
                    throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
                }
                else if (v.Equals("and", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.MoveNext() && context.Stack.Peek() is ConditionStatement vs)
                    {
                        var op = new ConditionsStatement() { Condition = Condition.And };
                        var index = context.Index;
                        op.Left = vs;
                        context.Stack.Pop();
                        context.Stack.Push(op);
                        do
                        {
                            context.Parse(context, true);
                            if (op.Right != null)
                            {
                                return true;
                            }
                            if (context.Stack.TryPeek(out var vsss) && vsss != op && vsss is ConditionStatement vss)
                            {
                                context.Stack.Pop();
                                op.Right = vss;
                                if (context.Stack.Peek() == op)
                                {
                                    return true;
                                }
                            }
                        } while (context.HasToken());
                        context.Index = index;
                        c = context.Current;
                    }
                    throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
                }
                else if (v.Equals("in", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.MoveNext() && context.Stack.Peek() is FieldStatement vs)
                    {
                        var op = new InOperaterStatement();
                        var index = context.Index;
                        op.Left = vs;
                        context.Stack.Pop();
                        context.Stack.Push(op);
                        if (ConvertArrary(context, op) && context.Stack.Peek() == op)
                        {
                            return true;
                        }
                        context.Index = index;
                        c = context.Current;
                    }
                    throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
                }
                else if (TryConvertDynmaicField(v, context))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryConvertDynmaicField(ReadOnlySpan<char> v, StatementParserContext context)
    {
        if (v.Equals("Query", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Header", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
            || v.Equals("Form", StringComparison.OrdinalIgnoreCase)
            //|| v.Equals("Route", StringComparison.OrdinalIgnoreCase)
            )
        {
            var index = context.Index;
            if (context.MoveNext())
            {
                var t = context.Current;
                if (t.Type == TokenType.Sign
                    && t.GetValue().Equals("(", StringComparison.Ordinal))
                {
                    var op = new DynamicFieldStatement() { Field = v.ToString() };
                    context.Stack.Push(op);
                    if (ConvertDynmaicField(context, op) && context.Stack.Peek() == op)
                    {
                        return true;
                    }
                    throw new ParserExecption($"Can't parse near by {t.GetValue()} (Line:{t.StartLine},Col:{t.StartColumn})");
                }
            }
            context.Index = index;
        }

        return false;
    }

    private bool ConvertDynmaicField(StatementParserContext context, DynamicFieldStatement op)
    {
        Token t;
        if (context.MoveNext())
        {
            t = context.Current;
            if (t.Type != TokenType.String)
            {
                return false;
            }
            op.Key = t.GetValue().ToString();
            if (!context.MoveNext())
            {
                return false;
            }
            t = context.Current;
            if (t.Type != TokenType.Sign && !t.GetValue().Equals(")", StringComparison.Ordinal))
            {
                return false;
            }
            context.MoveNext();
            return true;
        }
        return false;
    }

    private static bool ConvertSign(StatementParserContext context, ref Token c)
    {
        var v = c.GetValue();
        var index = context.Index;
        if (v.Equals("<", StringComparison.Ordinal)
            || v.Equals("<=", StringComparison.Ordinal)
            || v.Equals(">", StringComparison.Ordinal)
            || v.Equals(">=", StringComparison.Ordinal)
            || v.Equals("=", StringComparison.Ordinal)
            || v.Equals("!=", StringComparison.Ordinal)
            || v.Equals("~=", StringComparison.Ordinal))
        {
            var op = new OperaterStatement();
            op.Operater = v.ToString();
            if (context.MoveNext() && context.Stack.Peek() is ValueStatement vs)
            {
                op.Left = vs;
                context.Stack.Pop();
                context.Stack.Push(op);
                context.Parse(context, true);
                if (context.Stack.TryPop(out var vsss) && vsss is ValueStatement vss)
                {
                    //if ((op.Operater == "=" || op.Operater == "!=") && vss is FieldStatement f && f.Field.Equals("null", StringComparison.OrdinalIgnoreCase))
                    //{
                    //    op.Operater = op.Operater == "=" ? "is-null" : "not-null";
                    //}
                    //else
                    //{
                    //    op.Right = vss;
                    //}
                    op.Right = vss;

                    if (context.Stack.Peek() == op)
                    {
                        return true;
                    }
                }
            }
        }
        else if (v.Equals("(", StringComparison.Ordinal))
        {
            if (context.MoveNext())
            {
                var op = new OperaterStatement();
                context.Stack.Push(op);
                do
                {
                    context.Parse(context, true);
                    if (context.Current.Type == TokenType.Sign
                        && context.Current.GetValue().Equals(")", StringComparison.Ordinal))
                    {
                        context.MoveNext();
                        if (context.Stack.TryPop(out var s) && s is ConditionStatement css)
                        {
                            if (context.Stack.Peek() == op)
                            {
                                context.Stack.Pop();
                                if (context.Stack.TryPeek(out var p) && p is ConditionsStatement cs && cs.Right == null)
                                {
                                    cs.Right = css;
                                }
                                else
                                {
                                    context.Stack.Push(s);
                                }
                                return true;
                            }
                        }
                    }
                } while (context.HasToken());
            }
        }

        context.Index = index;
        c = context.Current;
        throw new ParserExecption($"Can't parse near by {c.GetValue()} (Line:{c.StartLine},Col:{c.StartColumn})");
    }

    private bool ConvertArrary(StatementParserContext context, InOperaterStatement iop)
    {
        var t = context.Current;
        if (t.Type == TokenType.Sign
            && t.GetValue().Equals("(", StringComparison.Ordinal)
            && context.MoveNext())
        {
            t = context.Current;
            if (t.Type == TokenType.Number)
            {
                if (ConvertNumberArrary(context, t, out var op))
                {
                    iop.Right = op;
                    return true;
                }
            }
            else if (t.Type == TokenType.String)
            {
                if (ConvertStringArrary(context, t, out var op))
                {
                    iop.Right = op;
                    return true;
                }
            }
            else if (t.Type == TokenType.True || t.Type == TokenType.False)
            {
                if (ConvertBoolArrary(context, t, out var op))
                {
                    iop.Right = op;
                    return true;
                }
            }
        }
        return false;
    }

    private static bool ConvertBoolArrary(StatementParserContext context, Token t, out ArrayValueStatement o)
    {
        var op = new BooleanArrayValueStatement() { Value = new List<bool>() { t.Type == TokenType.True } };
        o = op;
        var hasEnd = false;
        while (context.MoveNext())
        {
            t = context.Current;
            if (t.Type == TokenType.Sign)
            {
                var tv = t.GetValue();
                if (tv.Equals(",", StringComparison.Ordinal))
                {
                    if (context.MoveNext())
                    {
                        t = context.Current;
                        if (t.Type == TokenType.True)
                        {
                            op.Value.Add(true);
                            continue;
                        }
                        else if (t.Type == TokenType.False)
                        {
                            op.Value.Add(false);
                            continue;
                        }
                    }
                }
                else if (tv.Equals(")", StringComparison.Ordinal))
                {
                    context.MoveNext();
                    hasEnd = true;
                    break;
                }
            }

            break;
        }

        return hasEnd;
    }

    private static bool ConvertStringArrary(StatementParserContext context, Token t, out ArrayValueStatement o)
    {
        var op = new StringArrayValueStatement() { Value = new List<string>() { t.GetValue().ToString() } };
        o = op;
        var hasEnd = false;
        while (context.MoveNext())
        {
            t = context.Current;
            if (t.Type == TokenType.Sign)
            {
                var tv = t.GetValue();
                if (tv.Equals(",", StringComparison.Ordinal))
                {
                    if (context.MoveNext())
                    {
                        t = context.Current;
                        if (t.Type == TokenType.String)
                        {
                            op.Value.Add(t.GetValue().ToString());
                            continue;
                        }
                    }
                }
                else if (tv.Equals(")", StringComparison.Ordinal))
                {
                    context.MoveNext();
                    hasEnd = true;
                    break;
                }
            }

            break;
        }

        return hasEnd;
    }

    private static bool ConvertNumberArrary(StatementParserContext context, Token t, out ArrayValueStatement o)
    {
        var op = new NumberArrayValueStatement() { Value = new List<decimal>() { decimal.Parse(t.GetValue()) } };
        o = op;
        var hasEnd = false;
        while (context.MoveNext())
        {
            t = context.Current;
            if (t.Type == TokenType.Sign)
            {
                var tv = t.GetValue();
                if (tv.Equals(",", StringComparison.Ordinal))
                {
                    if (context.MoveNext())
                    {
                        t = context.Current;
                        if (t.Type == TokenType.Number)
                        {
                            op.Value.Add(decimal.Parse(t.GetValue()));
                            continue;
                        }
                    }
                }
                else if (tv.Equals(")", StringComparison.Ordinal))
                {
                    context.MoveNext();
                    hasEnd = true;
                    break;
                }
            }

            break;
        }

        return hasEnd;
    }
}
namespace VKProxy.HttpRoutingStatement.Statements;

public class BooleanValueStatement : ValueStatement
{
    public bool Value { get; set; }

    public static readonly BooleanValueStatement True = new BooleanValueStatement() { Value = true };

    public static readonly BooleanValueStatement False = new BooleanValueStatement() { Value = false };
}
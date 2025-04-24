namespace VKProxy.HttpRoutingStatement.Statements;

public class UnaryOperaterStatement : ConditionStatement
{
    public string Operater { get; set; }
    public ConditionStatement Right { get; set; }

    public override void Visit(Action<Statement> visitor)
    {
        visitor(this);
        Right?.Visit(visitor);
    }
}
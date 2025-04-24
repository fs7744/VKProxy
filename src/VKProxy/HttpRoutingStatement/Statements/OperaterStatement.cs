namespace VKProxy.HttpRoutingStatement.Statements;

public class OperaterStatement : ConditionStatement
{
    public ValueStatement Left { get; set; }
    public string Operater { get; set; }
    public ValueStatement Right { get; set; }

    public override void Visit(Action<Statement> visitor)
    {
        visitor(this);
        Left?.Visit(visitor);
        Right?.Visit(visitor);
    }
}
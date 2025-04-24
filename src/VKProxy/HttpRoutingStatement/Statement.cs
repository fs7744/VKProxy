namespace VKProxy.HttpRoutingStatement;

public abstract class Statement
{
    public virtual void Visit(Action<Statement> visitor)
    {
        visitor(this);
    }
}
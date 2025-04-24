namespace VKProxy.HttpRoutingStatement;

public interface IStatementParser
{
    bool TryParse(StatementParserContext context);
}
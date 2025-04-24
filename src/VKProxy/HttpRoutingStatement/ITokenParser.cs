namespace VKProxy.HttpRoutingStatement;

public interface ITokenParser
{
    bool TryTokenize(TokenParserContext context, out Token t);
}
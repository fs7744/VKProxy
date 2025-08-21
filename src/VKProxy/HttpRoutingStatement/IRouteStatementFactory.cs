using Microsoft.AspNetCore.Http;

namespace VKProxy.HttpRoutingStatement;

public interface IRouteStatementFactory
{
    Func<HttpContext, bool> ConvertToFunction(string statement);
}

public class DefaultRouteStatementFactory : IRouteStatementFactory
{
    public Func<HttpContext, bool> ConvertToFunction(string statement)
    {
        return HttpRoutingStatementParser.ConvertToFunction(statement);
    }
}
using Microsoft.AspNetCore.Http;

namespace VKProxy.HttpRoutingStatement;

public static partial class HttpRoutingStatementParser
{
    public static Func<HttpContext, bool> ConvertToFunc(Statement statement)
    {
        return null;
    }
}
using DotNext;
using Microsoft.AspNetCore.Http;
using VKProxy.HttpRoutingStatement;
using VKProxy.Middlewares.Http;

namespace VKProxy.Config.Validators;

public class RouteConfigValidator : IValidator<RouteConfig>
{
    private IHttpFunc[] funcs;
    private readonly HttpReverseProxy http;

    public RouteConfigValidator(IEnumerable<IHttpFunc> funcs, HttpReverseProxy http)
    {
        this.funcs = funcs.OrderByDescending(i => i.Order).ToArray();
        this.http = http;
    }

    public ValueTask<bool> ValidateAsync(RouteConfig? value, List<Exception> exceptions, CancellationToken cancellationToken)
    {
        if (value == null)
            return new ValueTask<bool>(false);

        var r = true;

        var match = value.Match;
        if (match != null)
        {
            if (!string.IsNullOrWhiteSpace(match.Statement))
            {
                try
                {
                    match.StatementFunc = HttpRoutingStatementParser.ConvertToFunction(match.Statement);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }

        if (!funcs.IsNullOrEmpty())
        {
            RequestDelegate f = http.Proxy;
            foreach (var func in funcs)
            {
                try
                {
                    f = func.Create(value, f);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (f == http.Proxy)
                value.HttpFunc = null;
            else
                value.HttpFunc = f;
        }

        return new ValueTask<bool>(r);
    }
}
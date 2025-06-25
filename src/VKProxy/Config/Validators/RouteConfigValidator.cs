using DotNext;
using Microsoft.AspNetCore.Http;
using VKProxy.HttpRoutingStatement;
using VKProxy.Middlewares.Http;

namespace VKProxy.Config.Validators;

public class RouteConfigValidator : IValidator<RouteConfig>
{
    private IHttpFunc[] funcs;
    public static RequestDelegate Nothing = c => Task.CompletedTask;

    public RouteConfigValidator(IEnumerable<IHttpFunc> funcs)
    {
        this.funcs = funcs.OrderByDescending(i => i.Order).ToArray();
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
                    match.StatementFunc = HttpRoutingStatementParser.ConvertToFunc(match.Statement);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }

        if (!funcs.IsNullOrEmpty())
        {
            value.HttpFunc = Nothing;
            foreach (var func in funcs)
            {
                try
                {
                    value.HttpFunc = func.Create(value, value.HttpFunc);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (value.HttpFunc == Nothing)
                value.HttpFunc = null;
        }

        return new ValueTask<bool>(r);
    }
}
using VKProxy.HttpRoutingStatement;

namespace VKProxy.Config.Validators;

public class RouteConfigValidator : IValidator<RouteConfig>
{
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

        return new ValueTask<bool>(r);
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

namespace VKProxy.Middlewares.Http.Transforms;

public class QueryTransformContext
{
    private readonly HttpRequest _request;
    private readonly QueryString _originalQueryString;
    private Dictionary<string, StringValues>? _modifiedQueryParameters;

    public QueryTransformContext(HttpRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _originalQueryString = request.QueryString;
        _modifiedQueryParameters = null;
    }

    public QueryString QueryString
    {
        get
        {
            if (_modifiedQueryParameters is null)
            {
                return _originalQueryString;
            }

            return new QueryBuilder(_modifiedQueryParameters).ToQueryString();
        }
    }

    public IDictionary<string, StringValues> Collection
    {
        get
        {
            if (_modifiedQueryParameters is null)
            {
                _modifiedQueryParameters = new Dictionary<string, StringValues>(_request.Query, StringComparer.OrdinalIgnoreCase);
            }

            return _modifiedQueryParameters;
        }
    }
}
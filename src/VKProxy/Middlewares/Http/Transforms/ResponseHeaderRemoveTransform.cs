namespace VKProxy.Middlewares.Http.Transforms;

public class ResponseHeaderRemoveTransform : ResponseTransform
{
    public ResponseHeaderRemoveTransform(string headerName, ResponseCondition condition)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        HeaderName = headerName;
        Condition = condition;
    }

    internal string HeaderName { get; }

    internal ResponseCondition Condition { get; }

    // Assumes the response status code has been set on the HttpContext already.
    /// <inheritdoc/>
    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (Condition == ResponseCondition.Always
            || Success(context) == (Condition == ResponseCondition.Success))
        {
            context.HttpContext.Response.Headers.Remove(HeaderName);
        }

        return default;
    }
}

internal class CorsResponseHeaderRemoveTransform : ResponseTransform, ITransformProvider
{
    public void Apply(TransformBuilderContext context)
    {
        context.ResponseTransforms.Add(this);
    }

    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        var items = context.HttpContext.Items;
        var respH = context.HttpContext.Response.Headers;
        if (items.ContainsKey("AccessControlAllowOrigin"))
        {
            var d = respH.AccessControlAllowOrigin;
            if (d.Count > 1)
            {
                respH.AccessControlAllowOrigin = d.First();
            }

            d = respH.AccessControlAllowHeaders;
            if (d.Count > 1)
            {
                respH.AccessControlAllowHeaders = d.First();
            }

            d = respH.AccessControlAllowMethods;
            if (d.Count > 1)
            {
                respH.AccessControlAllowMethods = d.First();
            }

            d = respH.AccessControlAllowCredentials;
            if (d.Count > 1)
            {
                respH.AccessControlAllowCredentials = d.First();
            }

            d = respH.AccessControlMaxAge;
            if (d.Count > 1)
            {
                respH.AccessControlMaxAge = d.First();
            }

            d = respH.AccessControlExposeHeaders;
            if (d.Count > 1)
            {
                respH.AccessControlExposeHeaders = d.First();
            }
        }
        return default;
    }
}
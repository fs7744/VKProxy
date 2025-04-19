namespace VKProxy.Middlewares.Http.Transforms;

public class RequestHeaderRemoveTransform : RequestTransform
{
    public RequestHeaderRemoveTransform(string headerName)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        HeaderName = headerName;
    }

    internal string HeaderName { get; }

    /// <inheritdoc/>
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        RemoveHeader(context, HeaderName);

        return default;
    }
}
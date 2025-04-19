namespace VKProxy.Middlewares.Http.Transforms;

public class QueryParameterRemoveTransform : RequestTransform
{
    public QueryParameterRemoveTransform(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
        }

        Key = key;
    }

    internal string Key { get; }

    /// <inheritdoc/>
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.Query.Collection.Remove(Key);

        return default;
    }
}
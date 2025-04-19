namespace VKProxy.Middlewares.Http.Transforms;

public class RequestHeaderValueTransform : RequestHeaderTransform
{
    public RequestHeaderValueTransform(string headerName, string value, bool append) : base(headerName, append)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            throw new ArgumentException($"'{nameof(headerName)}' cannot be null or empty.", nameof(headerName));
        }

        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal string Value { get; }

    /// <inheritdoc/>
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        return base.ApplyAsync(context);
    }

    /// <inheritdoc/>
    protected override string GetValue(RequestTransformContext context)
    {
        return Value;
    }
}
namespace VKProxy.Middlewares.Http.Transforms;

public sealed class RequestHeadersTransformFactory : ITransformFactory
{
    internal const string RequestHeadersCopyKey = "RequestHeadersCopy";
    internal const string RequestHeaderOriginalHostKey = "RequestHeaderOriginalHost";
    internal const string RequestHeaderKey = "RequestHeader";
    internal const string RequestHeaderRouteValueKey = "RequestHeaderRouteValue";
    internal const string RequestHeaderRemoveKey = "RequestHeaderRemove";
    internal const string RequestHeadersAllowedKey = "RequestHeadersAllowed";
    internal const string AppendKey = "Append";
    internal const string SetKey = "Set";

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue(RequestHeadersCopyKey, out var copyHeaders))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            if (bool.TryParse(copyHeaders, out var b))
            {
                context.CopyRequestHeaders = b;
            }
            else
            {
                context.Errors.Add(new ArgumentException($"Unexpected value for RequestHeaderCopy: {copyHeaders}. Expected 'true' or 'false'"));
            }
        }
        else if (transformValues.TryGetValue(RequestHeaderOriginalHostKey, out var originalHost))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            if (bool.TryParse(originalHost, out var b))
            {
                context.AddOriginalHost(b);
            }
            else
            {
                context.Errors.Add(new ArgumentException($"Unexpected value for RequestHeaderOriginalHost: {originalHost}. Expected 'true' or 'false'"));
            }
        }
        else if (transformValues.TryGetValue(RequestHeaderKey, out var headerName))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2);
            if (transformValues.TryGetValue(SetKey, out var setValue))
            {
                AddRequestHeader(context, headerName, setValue, append: false);
            }
            else if (transformValues.TryGetValue(AppendKey, out var appendValue))
            {
                AddRequestHeader(context, headerName, appendValue, append: true);
            }
            else
            {
                context.Errors.Add(new ArgumentException($"Unexpected parameters for RequestHeader: {string.Join(';', transformValues.Keys)}. Expected 'Set' or 'Append'"));
            }
        }
        else if (transformValues.TryGetValue(RequestHeaderRouteValueKey, out var headerNameFromRoute))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2);
            if (transformValues.TryGetValue(AppendKey, out var routeValueKeyAppend))
            {
                AddRequestHeaderRouteValue(context, headerNameFromRoute, routeValueKeyAppend, append: true);
            }
            else if (transformValues.TryGetValue(SetKey, out var routeValueKeySet))
            {
                AddRequestHeaderRouteValue(context, headerNameFromRoute, routeValueKeySet, append: false);
            }
            else
            {
                context.Errors.Add(new NotSupportedException(string.Join(";", transformValues.Keys)));
            }
        }
        else if (transformValues.TryGetValue(RequestHeaderRemoveKey, out var removeHeaderName))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            AddRequestHeaderRemove(context, removeHeaderName);
        }
        else if (transformValues.TryGetValue(RequestHeadersAllowedKey, out var allowedHeaders))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            var headersList = allowedHeaders.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AddRequestHeadersAllowed(context, headersList);
        }
        else
        {
            return false;
        }

        return true;
    }

    public static TransformBuilderContext AddRequestHeader(TransformBuilderContext context, string headerName, string value, bool append = true)
    {
        context.RequestTransforms.Add(new RequestHeaderValueTransform(headerName, value, append));
        return context;
    }

    public static TransformBuilderContext AddRequestHeaderRouteValue(TransformBuilderContext context, string headerName, string routeValueKey, bool append = true)
    {
        context.RequestTransforms.Add(new RequestHeaderRouteValueTransform(headerName, routeValueKey, append));
        return context;
    }

    public static TransformBuilderContext AddRequestHeaderRemove(TransformBuilderContext context, string headerName)
    {
        context.RequestTransforms.Add(new RequestHeaderRemoveTransform(headerName));
        return context;
    }

    public static TransformBuilderContext AddRequestHeadersAllowed(TransformBuilderContext context, params string[] allowedHeaders)
    {
        context.CopyRequestHeaders = false;
        context.RequestTransforms.Add(new RequestHeadersAllowedTransform(allowedHeaders));
        return context;
    }
}
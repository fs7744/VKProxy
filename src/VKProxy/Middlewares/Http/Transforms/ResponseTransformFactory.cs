namespace VKProxy.Middlewares.Http.Transforms;

internal sealed class ResponseTransformFactory : ITransformFactory
{
    internal const string ResponseHeadersCopyKey = "ResponseHeadersCopy";
    internal const string ResponseTrailersCopyKey = "ResponseTrailersCopy";
    internal const string ResponseHeaderKey = "ResponseHeader";
    internal const string ResponseTrailerKey = "ResponseTrailer";
    internal const string ResponseHeaderRemoveKey = "ResponseHeaderRemove";
    internal const string ResponseTrailerRemoveKey = "ResponseTrailerRemove";
    internal const string ResponseHeadersAllowedKey = "ResponseHeadersAllowed";
    internal const string ResponseTrailersAllowedKey = "ResponseTrailersAllowed";
    internal const string WhenKey = "When";
    internal const string AppendKey = "Append";
    internal const string SetKey = "Set";

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue(ResponseHeadersCopyKey, out var copyHeaders))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            if (bool.TryParse(copyHeaders, out var b))
            {
                context.CopyResponseHeaders = b;
            }
            else
            {
                context.Errors.Add(new ArgumentException($"Unexpected value for ResponseHeadersCopy: {copyHeaders}. Expected 'true' or 'false'"));
            }
        }
        else if (transformValues.TryGetValue(ResponseTrailersCopyKey, out copyHeaders))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            if (bool.TryParse(copyHeaders, out var b))
            {
                context.CopyResponseTrailers = b;
            }
            else
            {
                context.Errors.Add(new ArgumentException($"Unexpected value for ResponseTrailersCopy: {copyHeaders}. Expected 'true' or 'false'"));
            }
        }
        else if (transformValues.TryGetValue(ResponseHeaderKey, out var responseHeaderName))
        {
            var condition = ResponseCondition.Success;
            if (transformValues.TryGetValue(WhenKey, out var whenValue))
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 3);
                if (Enum.TryParse<ResponseCondition>(whenValue, ignoreCase: true, out var e))
                {
                    condition = e;
                }
            }
            else
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2);
            }

            if (transformValues.TryGetValue(SetKey, out var setValue))
            {
                AddResponseHeader(context, responseHeaderName, setValue, append: false, condition);
            }
            else if (transformValues.TryGetValue(AppendKey, out var appendValue))
            {
                AddResponseHeader(context, responseHeaderName, appendValue, append: true, condition);
            }
            else
            {
                context.Errors.Add(new ArgumentException($"Unexpected parameters for ResponseHeader: {string.Join(';', transformValues.Keys)}. Expected 'Set' or 'Append'"));
            }
        }
        else if (transformValues.TryGetValue(ResponseTrailerKey, out var responseTrailerName))
        {
            var condition = ResponseCondition.Success;
            if (transformValues.TryGetValue(WhenKey, out var whenValue))
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 3);
                if (Enum.TryParse<ResponseCondition>(whenValue, ignoreCase: true, out var e))
                {
                    condition = e;
                }
            }
            else
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2);
            }

            if (transformValues.TryGetValue(SetKey, out var setValue))
            {
                AddResponseTrailer(context, responseTrailerName, setValue, append: false, condition);
            }
            else if (transformValues.TryGetValue(AppendKey, out var appendValue))
            {
                AddResponseTrailer(context, responseTrailerName, appendValue, append: true, condition);
            }
            else
            {
                context.Errors.Add(new ArgumentException($"Unexpected parameters for ResponseTrailer: {string.Join(';', transformValues.Keys)}. Expected 'Set' or 'Append'"));
            }
        }
        else if (transformValues.TryGetValue(ResponseHeaderRemoveKey, out var removeResponseHeaderName))
        {
            var condition = ResponseCondition.Success;
            if (transformValues.TryGetValue(WhenKey, out var whenValue))
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2);
                if (Enum.TryParse<ResponseCondition>(whenValue, ignoreCase: true, out var e))
                {
                    condition = e;
                }
            }
            else
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            }

            AddResponseHeaderRemove(context, removeResponseHeaderName, condition);
        }
        else if (transformValues.TryGetValue(ResponseTrailerRemoveKey, out var removeResponseTrailerName))
        {
            var condition = ResponseCondition.Success;
            if (transformValues.TryGetValue(WhenKey, out var whenValue))
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2);
                if (Enum.TryParse<ResponseCondition>(whenValue, ignoreCase: true, out var e))
                {
                    condition = e;
                }
            }
            else
            {
                TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            }

            AddResponseTrailerRemove(context, removeResponseTrailerName, condition);
        }
        else if (transformValues.TryGetValue(ResponseHeadersAllowedKey, out var allowedHeaders))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            var headersList = allowedHeaders.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AddResponseHeadersAllowed(context, headersList);
        }
        else if (transformValues.TryGetValue(ResponseTrailersAllowedKey, out var allowedTrailers))
        {
            TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1);
            var headersList = allowedTrailers.Split(';', options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AddResponseTrailersAllowed(context, headersList);
        }
        else
        {
            return false;
        }

        return true;
    }

    public static TransformBuilderContext AddResponseHeader(TransformBuilderContext context, string headerName, string value, bool append = true, ResponseCondition condition = ResponseCondition.Success)
    {
        context.ResponseTransforms.Add(new ResponseHeaderValueTransform(headerName, value, append, condition));
        return context;
    }

    public static TransformBuilderContext AddResponseTrailer(TransformBuilderContext context, string headerName, string value, bool append = true, ResponseCondition condition = ResponseCondition.Success)
    {
        context.ResponseTrailersTransforms.Add(new ResponseTrailerValueTransform(headerName, value, append, condition));
        return context;
    }

    public static TransformBuilderContext AddResponseHeaderRemove(TransformBuilderContext context, string headerName, ResponseCondition condition = ResponseCondition.Success)
    {
        context.ResponseTransforms.Add(new ResponseHeaderRemoveTransform(headerName, condition));
        return context;
    }

    public static TransformBuilderContext AddResponseTrailerRemove(TransformBuilderContext context, string headerName, ResponseCondition condition = ResponseCondition.Success)
    {
        context.ResponseTrailersTransforms.Add(new ResponseTrailerRemoveTransform(headerName, condition));
        return context;
    }

    public static TransformBuilderContext AddResponseHeadersAllowed(TransformBuilderContext context, params string[] allowedHeaders)
    {
        context.CopyResponseHeaders = false;
        context.ResponseTransforms.Add(new ResponseHeadersAllowedTransform(allowedHeaders));
        return context;
    }

    public static TransformBuilderContext AddResponseTrailersAllowed(TransformBuilderContext context, params string[] allowedHeaders)
    {
        context.CopyResponseTrailers = false;
        context.ResponseTrailersTransforms.Add(new ResponseTrailersAllowedTransform(allowedHeaders));
        return context;
    }
}
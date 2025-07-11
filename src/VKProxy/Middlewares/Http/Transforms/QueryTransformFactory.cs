namespace VKProxy.Middlewares.Http.Transforms;

public sealed class QueryTransformFactory : ITransformFactory
{
    internal const string QueryValueParameterKey = "QueryValueParameter";
    internal const string QueryRouteParameterKey = "QueryRouteParameter";
    internal const string QueryRemoveParameterKey = "QueryRemoveParameter";
    internal const string AppendKey = "Append";
    internal const string SetKey = "Set";

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue(QueryValueParameterKey, out var queryValueParameter))
        {
            if (TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2))
            {
                if (transformValues.TryGetValue(AppendKey, out var appendValue))
                {
                    AddQueryValue(context, queryValueParameter, appendValue, append: true);
                }
                else if (transformValues.TryGetValue(SetKey, out var setValue))
                {
                    AddQueryValue(context, queryValueParameter, setValue, append: false);
                }
                else
                {
                    context.Errors.Add(new NotSupportedException(string.Join(";", transformValues.Keys)));
                    return false;
                }
            }
            else
                return false;
        }
        else if (transformValues.TryGetValue(QueryRouteParameterKey, out var queryRouteParameter))
        {
            if (TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2))
            {
                if (transformValues.TryGetValue(AppendKey, out var routeValueKeyAppend))
                {
                    AddQueryRouteValue(context, queryRouteParameter, routeValueKeyAppend, append: true);
                }
                else if (transformValues.TryGetValue(SetKey, out var routeValueKeySet))
                {
                    AddQueryRouteValue(context, queryRouteParameter, routeValueKeySet, append: false);
                }
                else
                {
                    context.Errors.Add(new NotSupportedException(string.Join(";", transformValues.Keys)));
                }
            }
            else
                return false;
        }
        else if (transformValues.TryGetValue(QueryRemoveParameterKey, out var removeQueryParameter))
        {
            if (TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1))
            {
                AddQueryRemoveKey(context, removeQueryParameter);
            }
            else
            {
                return false;
            }
        }
        else
            return false;

        return true;
    }

    public static TransformBuilderContext AddQueryValue(TransformBuilderContext context, string queryKey, string value, bool append = true)
    {
        context.RequestTransforms.Add(new QueryParameterFromStaticTransform(
            append ? QueryStringTransformMode.Append : QueryStringTransformMode.Set,
            queryKey, value));
        return context;
    }

    public static TransformBuilderContext AddQueryRouteValue(TransformBuilderContext context, string queryKey, string routeValueKey, bool append = true)
    {
        context.RequestTransforms.Add(new QueryParameterRouteTransform(
            append ? QueryStringTransformMode.Append : QueryStringTransformMode.Set,
            queryKey, routeValueKey));
        return context;
    }

    public static TransformBuilderContext AddQueryRemoveKey(TransformBuilderContext context, string queryKey)
    {
        context.RequestTransforms.Add(new QueryParameterRemoveTransform(queryKey));
        return context;
    }
}
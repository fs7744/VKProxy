namespace VKProxy.Middlewares.Http.Transforms;

public class HttpMethodTransformFactory : ITransformFactory
{
    internal const string HttpMethodChangeKey = "HttpMethodChange";
    internal const string SetKey = "Set";

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue(HttpMethodChangeKey, out var fromHttpMethod))
        {
            if (TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 2))
            {
                if (transformValues.TryGetValue(SetKey, out var toHttpMethod))
                {
                    AddHttpMethodChange(context, fromHttpMethod, toHttpMethod);
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    public static TransformBuilderContext AddHttpMethodChange(TransformBuilderContext context, string fromHttpMethod, string toHttpMethod)
    {
        context.RequestTransforms.Add(new HttpMethodChangeTransform(fromHttpMethod, toHttpMethod));
        return context;
    }
}
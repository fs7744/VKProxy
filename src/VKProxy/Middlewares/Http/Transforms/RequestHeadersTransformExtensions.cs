namespace VKProxy.Middlewares.Http.Transforms;

public static class RequestHeadersTransformExtensions
{
    public static TransformBuilderContext AddOriginalHost(this TransformBuilderContext context, bool useOriginal = true)
    {
        if (useOriginal)
        {
            context.RequestTransforms.Add(RequestHeaderOriginalHostTransform.OriginalHost);
        }
        else
        {
            context.RequestTransforms.Add(RequestHeaderOriginalHostTransform.SuppressHost);
        }
        return context;
    }
}
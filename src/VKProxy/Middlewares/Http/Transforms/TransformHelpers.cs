namespace VKProxy.Middlewares.Http.Transforms;

public static class TransformHelpers
{
    public static bool CheckTooManyParameters(TransformBuilderContext context, IReadOnlyDictionary<string, string> rawTransform, int expected)
    {
        if (rawTransform.Count > expected)
        {
            context.Errors.Add(new InvalidOperationException("The transform contains more parameters than expected: " + string.Join(';', rawTransform.Keys)));
            return false;
        }
        return true;
    }

    internal static void RemoveAllXForwardedHeaders(TransformBuilderContext context, string prefix)
    {
        context.AddXForwardedFor(prefix + ForwardedTransformFactory.ForKey, ForwardedTransformActions.Remove);
        context.AddXForwardedPrefix(prefix + ForwardedTransformFactory.PrefixKey, ForwardedTransformActions.Remove);
        context.AddXForwardedHost(prefix + ForwardedTransformFactory.HostKey, ForwardedTransformActions.Remove);
        context.AddXForwardedProto(prefix + ForwardedTransformFactory.ProtoKey, ForwardedTransformActions.Remove);
    }

    internal static void RemoveForwardedHeader(TransformBuilderContext context)
    {
        context.RequestTransforms.Add(RequestHeaderForwardedTransform.RemoveTransform);
    }
}
using Microsoft.AspNetCore.Http;
using VKProxy.Features;

namespace VKProxy.Middlewares.Http.Transforms;

public static class TransformHelpers
{
    public static readonly object ResponseTransformed = new object();

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

    public static bool HasResponseTransformed(this HttpContext context)
    {
        return context.Items.ContainsKey(ResponseTransformed);
    }

    public static void SetResponseTransformed(this HttpContext context)
    {
        context.Items[ResponseTransformed] = ResponseTransformed;
    }

    public static async Task DoHttpResponseTransformAsync(this HttpContext context)
    {
        if (context.HasResponseTransformed())
            return;

        var proxyFeature = context.Features.Get<IReverseProxyFeature>();
        if (proxyFeature is IL7ReverseProxyFeature l7)
        {
            var route = proxyFeature.Route;

            if (route is not null)
            {
                if (route.Transformer is not null)
                {
                    await route.Transformer.TransformResponseAsync(context, new HttpResponseMessage(), CancellationToken.None);
                }
            }
        }
    }
}
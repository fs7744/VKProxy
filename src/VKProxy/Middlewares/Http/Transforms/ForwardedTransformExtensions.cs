namespace VKProxy.Middlewares.Http.Transforms;

public static class ForwardedTransformExtensions
{
    /// <summary>
    /// Adds the transform which will add X-Forwarded-* request headers.
    /// </summary>
    /// <remarks>
    /// Also optionally removes the <c>Forwarded</c> header when enabled.
    /// </remarks>
    public static TransformBuilderContext AddXForwarded(this TransformBuilderContext context, ForwardedTransformActions action = ForwardedTransformActions.Set, bool removeForwardedHeader = true)
    {
        context.AddXForwardedFor(action: action);
        context.AddXForwardedPrefix(action: action);
        context.AddXForwardedHost(action: action);
        context.AddXForwardedProto(action: action);

        if (removeForwardedHeader)
        {
            // Remove the Forwarded header when an X-Forwarded transform is enabled
            TransformHelpers.RemoveForwardedHeader(context);
        }

        return context;
    }

    /// <summary>
    /// Adds the transform which will add X-Forwarded-Proto request header.
    /// </summary>
    public static TransformBuilderContext AddXForwardedProto(this TransformBuilderContext context, string headerName = "X-Forwarded-Proto", ForwardedTransformActions action = ForwardedTransformActions.Set)
    {
        context.UseDefaultForwarders = false;
        if (action == ForwardedTransformActions.Off)
        {
            return context;
        }
        context.RequestTransforms.Add(new RequestHeaderXForwardedProtoTransform(headerName, action));
        return context;
    }

    /// <summary>
    /// Adds the transform which will add X-Forwarded-Host request header.
    /// </summary>
    public static TransformBuilderContext AddXForwardedHost(this TransformBuilderContext context, string headerName = "X-Forwarded-Host", ForwardedTransformActions action = ForwardedTransformActions.Set)
    {
        context.UseDefaultForwarders = false;
        if (action == ForwardedTransformActions.Off)
        {
            return context;
        }
        context.RequestTransforms.Add(new RequestHeaderXForwardedHostTransform(headerName, action));
        return context;
    }

    /// <summary>
    /// Adds the transform which will add X-Forwarded-Prefix request header.
    /// </summary>
    public static TransformBuilderContext AddXForwardedPrefix(this TransformBuilderContext context, string headerName = "X-Forwarded-Prefix", ForwardedTransformActions action = ForwardedTransformActions.Set)
    {
        context.UseDefaultForwarders = false;
        if (action == ForwardedTransformActions.Off)
        {
            return context;
        }
        context.RequestTransforms.Add(new RequestHeaderXForwardedPrefixTransform(headerName, action));
        return context;
    }

    /// <summary>
    /// Adds the transform which will add X-Forwarded-For request header.
    /// </summary>
    public static TransformBuilderContext AddXForwardedFor(this TransformBuilderContext context, string headerName = "X-Forwarded-For", ForwardedTransformActions action = ForwardedTransformActions.Set)
    {
        context.UseDefaultForwarders = false;
        if (action == ForwardedTransformActions.Off)
        {
            return context;
        }
        context.RequestTransforms.Add(new RequestHeaderXForwardedForTransform(headerName, action));
        return context;
    }

    public static TransformBuilderContext AddClientCertHeader(this TransformBuilderContext context, string headerName)
    {
        context.RequestTransforms.Add(new RequestHeaderClientCertTransform(headerName));
        return context;
    }
}
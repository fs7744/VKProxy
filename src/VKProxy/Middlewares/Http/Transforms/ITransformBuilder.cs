using VKProxy.Config;

namespace VKProxy.Middlewares.Http.Transforms;

public interface ITransformBuilder
{
    IHttpTransformer Build(RouteConfig route, List<Exception> exceptions);
}

public class TransformBuilder : ITransformBuilder
{
    private readonly IServiceProvider services;
    private readonly IEnumerable<ITransformFactory> factories;
    private readonly IEnumerable<ITransformProvider> providers;

    public TransformBuilder(IServiceProvider services, IEnumerable<ITransformFactory> factories, IEnumerable<ITransformProvider> providers)
    {
        this.services = services;
        this.factories = factories;
        this.providers = providers;
    }

    public IHttpTransformer Build(RouteConfig route, List<Exception> exceptions)
    {
        var context = new TransformBuilderContext
        {
            Services = services,
            Route = route,
            Cluster = route.ClusterConfig,
            Errors = exceptions
        };

        var rawTransforms = route.Transforms;

        if (rawTransforms?.Count > 0)
        {
            foreach (var rawTransform in rawTransforms)
            {
                var handled = false;
                foreach (var factory in factories)
                {
                    if (factory.Build(context, rawTransform))
                    {
                        handled = true;
                        break;
                    }
                }

                if (!handled)
                {
                    exceptions.Add(new ArgumentException($"Unknown transform: {string.Join(';', rawTransform.Keys)}"));
                }
            }
        }

        foreach (var transformProvider in providers)
        {
            transformProvider.Apply(context);
        }

        return CreateTransformer(context);
    }

    internal static StructuredTransformer CreateTransformer(TransformBuilderContext context)
    {
        // RequestHeaderOriginalHostKey defaults to false, and CopyRequestHeaders defaults to true.
        // If RequestHeaderOriginalHostKey was not specified then we need to make sure the transform gets
        // added anyway to remove the original host and to observe hosts specified in DestinationConfig.
        if (!context.RequestTransforms.Any(item => item is RequestHeaderOriginalHostTransform))
        {
            context.AddOriginalHost(false);
        }

        // Add default forwarders only if they haven't already been added or disabled.
        if (context.UseDefaultForwarders.GetValueOrDefault(true))
        {
            context.AddXForwarded();
        }

        return new StructuredTransformer(
            context.CopyRequestHeaders,
            context.CopyResponseHeaders,
            context.CopyResponseTrailers,
            context.RequestTransforms,
            context.ResponseTransforms,
            context.ResponseTrailersTransforms);
    }
}
using VKProxy.Middlewares.Http.Transforms;

internal class AiGatewayTransformProvider : ITransformProvider
{
    public void Apply(TransformBuilderContext context)
    {
        context.RequestTransforms.Add(new AiGatewayRequestTransform());
    }
}
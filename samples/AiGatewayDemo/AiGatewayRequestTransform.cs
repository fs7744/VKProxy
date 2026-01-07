using VKProxy.Middlewares.Http.Transforms;

internal class AiGatewayRequestTransform : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {

        return ValueTask.CompletedTask;
    }
}
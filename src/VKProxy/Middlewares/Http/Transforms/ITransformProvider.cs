namespace VKProxy.Middlewares.Http.Transforms;

public interface ITransformProvider
{
    void Apply(TransformBuilderContext context);
}
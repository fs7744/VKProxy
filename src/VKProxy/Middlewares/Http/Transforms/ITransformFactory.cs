namespace VKProxy.Middlewares.Http.Transforms;

public interface ITransformFactory
{
    bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues);
}
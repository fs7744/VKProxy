using VKProxy.Middlewares.Http.Transforms;

namespace ProxyDemo.Transforms;

internal class TestTransformFactory : ITransformFactory
{
    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue("myHeader", out var v))  // 配置要满足有 myHeader
        {
            context.ResponseTransforms.Add(new TestAddResponseHeader(v));

            return true;
        }
        return false;
    }
}
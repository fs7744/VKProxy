using VKProxy.Middlewares.Http.Transforms;

namespace ProxyDemo.Transforms;

internal class TestITransformProvider : ITransformProvider
{
    public void Apply(TransformBuilderContext context) // 全部都会运行
    {
        context.ResponseTransforms.Add(new TestAddResponseHeader("all"));
    }
}
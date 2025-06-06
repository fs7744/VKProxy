using VKProxy.Middlewares.Http.Transforms;

namespace ProxyDemo.Transforms;

public class TestAddResponseHeader : ResponseTransform
{
    private string v;

    public TestAddResponseHeader(string v)
    {
        this.v = v;
    }

    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        SetHeader(context, $"x-{v}", v);

        return ValueTask.CompletedTask;
    }
}

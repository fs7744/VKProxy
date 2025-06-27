using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.ObjectPool;
using Moq;
using VKProxy.HttpRoutingStatement;
using VKProxy.TemplateStatement;

namespace UT.TemplateStatement;

public class TemplateStatementTokenParserTest
{
    private TemplateStatementFactory f = new TemplateStatementFactory(new DefaultObjectPoolProvider());

    [Theory]
    [InlineData("", "")]
    [InlineData("}", "Sign,}")]
    [InlineData("}}}", "Sign,}}Sign,}")]
    [InlineData("{", "Sign,{")]
    [InlineData("{{{", "Sign,{{Sign,{")]
    [InlineData("{{header('x-c')}}", "Sign,{{Word,header('x-c')Sign,}}")]
    public void ShouldParse(string test, string expected)
    {
        var a = string.Join("", f.Tokenize(test).Select(i => i.ToString()));
        Assert.Equal(expected, a);
    }

    [Theory]
    [InlineData("dadasdasd", "dadasdasd")]
    [InlineData("{Path}", "/testp")]
    [InlineData("{Header('x-c')}", "a")]
    [InlineData("{Query('x-c')}", "xxx")]
    [InlineData("{Cookie('x-c')}", "ddd")]
    [InlineData("{Path}#{Cookie('x-c')}", "/testp#ddd")]
    [InlineData("{Path}#{{}}{Cookie('x-c')}", "/testp#{}ddd")]
    public void ShouldConvert(string test, string expected)
    {
        var context = new Mock<HttpContext>();
        context.Setup(r => r.Request.Path).Returns("/testp");
        context.Setup(r => r.Request.ContentLength).Returns(9);
        context.Setup(r => r.Request.Headers["x-c"]).Returns("a");
        context.Setup(r => r.Request.Cookies["x-c"]).Returns("ddd");
        context.Setup(r => r.Request.Query["x-c"]).Returns("xxx");
        var a = f.Convert(test);
        Assert.Equal(expected.ToUpperInvariant(), a(context.Object));
    }

    [Theory]
    [InlineData("")]
    public void ShouldNull(string test)
    {
        var a = f.Convert(test);
        Assert.Null(a);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("{ {} }")]
    [InlineData("{ dsd{}dds }")]
    public void ShouldThrows(string test)
    {
        var c = Assert.Throws<ParserExecption>(() => f.Convert(test));
    }
}
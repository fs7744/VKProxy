using Microsoft.AspNetCore.Http;
using VKProxy;

namespace UT.LuaTest;

public class LuaTemplateEngineFactoryTest
{
    private readonly TemplateEngineFactoryRoot te;
    private DefaultHttpContext HttpContext;

    public LuaTemplateEngineFactoryTest()
    {
        te = new TemplateEngineFactoryRoot();
        this.HttpContext = new DefaultHttpContext();
        var req = HttpContext.Request;
        req.Path = "/testp/dsd/fsdfx/fadasd3/中";
        req.Method = "GET";
        req.Host = new HostString("x.com");
        req.Scheme = "https";
        req.Protocol = "HTTP/1.1";
        req.ContentType = "json";
        req.QueryString = new QueryString("?s=123&d=456&f=789");
        req.IsHttps = true;
        req.ContentLength = 1;
        HttpContext.TraceIdentifier = "t111";
        HttpContext.Response.ContentLength = 1;
        HttpContext.Response.ContentType = "json";
        HttpContext.Response.StatusCode = 400;
        for (int i = 0; i < 10; i++)
        {
            req.Headers.Add($"x-{i}", new string[] { $"v-{i}", $"x-{i}", $"s-{i}" });
            HttpContext.Response.Headers.Add($"x-{i}", new string[] { $"v-{i}", $"x-{i}", $"s-{i}" });
        }

        req.Headers.Cookie = "a=sss;b=444;";
        req.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { { "aa", "ddd" }, { "cc", "cccs" } });
    }


    [Theory]
    [InlineData("""
        local function f(c)
            return c.Request.Path.Value:match("中$") ~= nil
        end
        
        return f
        """, true)]
    [InlineData("""
        local function f(c)
            return c.Request.Path.Value:match("1中$") ~= nil
        end
        
        return f
        """, false)]
    [InlineData("Request.Path == '/testp/dsd/fsdfx/fadasd3/中'", true)]
    public void ConvertRouteFunctionTest(string func, bool r)
    {
        Assert.Equal(r, te.ConvertRouteFunction(func)(HttpContext));
    }

    [Theory]
    [InlineData("""
        local function f(c)
            return c.Request.Path.Value
        end
        
        return f
        """, "/testp/dsd/fsdfx/fadasd3/中")]
    [InlineData("{{Request.Path}}", "/testp/dsd/fsdfx/fadasd3/中")]
    public void ConvertTemplateTest(string func, string r)
    {
        Assert.Equal(r, te.ConvertTemplate(func)(HttpContext));
    }
}

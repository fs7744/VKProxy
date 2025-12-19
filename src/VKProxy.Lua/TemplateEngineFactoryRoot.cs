using Lmzzz.AspNetCoreTemplate;
using Microsoft.AspNetCore.Http;

namespace VKProxy;

public class TemplateEngineFactoryRoot : ITemplateEngineFactory
{
    private readonly ITemplateEngineFactory defaultTemplate = new DefaultTemplateEngineFactory();
    private readonly ITemplateEngineFactory lua = new LuaTemplateEngineFactory();

    public Func<HttpContext, bool> ConvertRouteFunction(string statement)
    {
        try
        {
            return lua.ConvertRouteFunction(statement);
        }
        catch (Exception)
        {
            return defaultTemplate.ConvertRouteFunction(statement);
        }
    }

    public Func<HttpContext, string> ConvertTemplate(string template)
    {
        try
        {
            return lua.ConvertTemplate(template);
        }
        catch (Exception)
        {
            return defaultTemplate.ConvertTemplate(template);
        }
    }
}

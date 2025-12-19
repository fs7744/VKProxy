using Lmzzz.AspNetCoreTemplate;
using Microsoft.AspNetCore.Http;
using NLua;
using System.Text;

namespace VKProxy;

public class LuaTemplateEngineFactory : ITemplateEngineFactory
{
    [ThreadStatic]
    private static Lua state;

    private static Lock _lock = new Lock();

    public static Lua InitNlua()
    {
        if (state == null)
        {
            lock (_lock)
            {
                if (state == null)
                {
                    var r = state = new Lua();
                    r.State.Encoding = Encoding.UTF8;
                    r.LoadCLRPackage();
                }
            }
        }
        return state;
    }

    public Func<HttpContext, bool> ConvertRouteFunction(string statement)
    {
        var ss = InitNlua();
        var fid = Guid.NewGuid().ToString();
        var g = ss.GetTable("_G");
        g[fid] = ss.DoString(statement)[0] as LuaFunction;
        return c =>
        {
            var s = InitNlua();
            var g = s.GetTable("_G");
            var f = g[fid] as LuaFunction;
            if (f == null)
            {
                f = ss.DoString(statement)[0] as LuaFunction;
                g[fid] = f;
            }
            if (f == null)
            {
                return false;
            }
            var r = f.Call(c);
            return (bool)r.First();
        };
    }

    public Func<HttpContext, string> ConvertTemplate(string template)
    {
        var ss = InitNlua();
        var fid = Guid.NewGuid().ToString();
        var g = ss.GetTable("_G");
        g[fid] = ss.DoString(template)[0] as LuaFunction;
        return c =>
        {
            var s = InitNlua();
            var g = s.GetTable("_G");
            var f = g[fid] as LuaFunction;
            if (f == null)
            {
                f = ss.DoString(template)[0] as LuaFunction;
                g[fid] = f;
            }
            if (f == null)
            {
                return null;
            }
            return (string)f.Call(c).First();
        };
    }
}
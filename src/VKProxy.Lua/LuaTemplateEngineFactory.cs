using Lmzzz.AspNetCoreTemplate;
using Microsoft.AspNetCore.Http;
using NLua;
using System.Text;

namespace VKProxy;

public class LuaTemplateEngineFactory : ITemplateEngineFactory
{
    //[ThreadStatic]
    //private static Lua state;

    //private static Lock _lock = new Lock();

    //public static Lua InitNlua()
    //{
    //    if (state == null)
    //    {
    //        lock (_lock)
    //        {
    //            if (state == null)
    //            {
    //                var r = state = new Lua();
    //                r.State.Encoding = Encoding.UTF8;
    //                r.LoadCLRPackage();
    //            }
    //        }
    //    }
    //    return state;
    //}

    /// <summary>
    /// 性能存在问题，不过能保证内存回收
    /// </summary>
    /// <param name="statement"></param>
    /// <returns></returns>
    public Func<HttpContext, bool> ConvertRouteFunction(string statement)
    {
        using (var lua = new Lua())
        {
            lua.LoadCLRPackage();
            var f = lua.DoString(statement)[0] as LuaFunction;
            if (f != null)
            {
                return c =>
                {
                    using (var l = new Lua())
                    {
                        l.State.Encoding = Encoding.UTF8;
                        l.LoadCLRPackage();
                        var ff = l.DoString(statement)[0] as LuaFunction;
                        var r = ff.Call(c);
                        return (bool)r.First();
                    }
                };
            }
            else
            {
                throw new Exception("Lua parse failed");
            }
        }
        //var ss = InitNlua();
        //var fid = Guid.NewGuid().ToString();
        //var g = ss.GetTable("_G");
        //g[fid] = ss.DoString(statement)[0] as LuaFunction;
        //return c =>
        //{
        //    var s = InitNlua();
        //    var g = s.GetTable("_G");
        //    var f = g[fid] as LuaFunction;
        //    if (f == null)
        //    {
        //        f = ss.DoString(statement)[0] as LuaFunction;
        //        g[fid] = f;
        //    }
        //    if (f == null)
        //    {
        //        return false;
        //    }
        //    var r = f.Call(c);
        //    return (bool)r.First();
        //};
    }

    public Func<HttpContext, string> ConvertTemplate(string template)
    {
        using (var lua = new Lua())
        {
            lua.LoadCLRPackage();
            var f = lua.DoString(template)[0] as LuaFunction;
            if (f != null)
            {
                return c =>
                {
                    using (var l = new Lua())
                    {
                        l.State.Encoding = Encoding.UTF8;
                        l.LoadCLRPackage();
                        var ff = l.DoString(template)[0] as LuaFunction;
                        var r = ff.Call(c);
                        return (string)r.First();
                    }
                };
            }
            else
            {
                throw new Exception("Lua parse failed");
            }
        }
        //var ss = InitNlua();
        //var fid = Guid.NewGuid().ToString();
        //var g = ss.GetTable("_G");
        //g[fid] = ss.DoString(template)[0] as LuaFunction;
        //return c =>
        //{
        //    var s = InitNlua();
        //    var g = s.GetTable("_G");
        //    var f = g[fid] as LuaFunction;
        //    if (f == null)
        //    {
        //        f = ss.DoString(template)[0] as LuaFunction;
        //        g[fid] = f;
        //    }
        //    if (f == null)
        //    {
        //        return null;
        //    }
        //    return (string)f.Call(c).First();
        //};
    }
}
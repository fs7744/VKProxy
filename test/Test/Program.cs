using DotNext.Runtime.Caching;
using VKProxy.Config;

//var cache = new RandomAccessCache<string, RouteConfig>(10000);
var a = new Dictionary<string, RouteConfig>(10000);

while (true)
{
    //cache.Dispose();
    //cache = new RandomAccessCache<string, RouteConfig>(10000);
    a = new Dictionary<string, RouteConfig>(10000);
    await Task.Delay(1000);
}
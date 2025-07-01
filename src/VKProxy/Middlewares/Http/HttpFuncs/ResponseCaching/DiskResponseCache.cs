using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public class DiskResponseCache : IResponseCache
{
    public string Name => "Disk";

    public ValueTask<IResponseCacheEntry?> GetAsync(string key, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetAsync(string key, IResponseCacheEntry entry, TimeSpan validFor, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
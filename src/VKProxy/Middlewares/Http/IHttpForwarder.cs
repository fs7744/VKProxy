using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VKProxy.Config;
using VKProxy.Middlewares.Http.Transforms;

namespace VKProxy.Middlewares.Http;

public interface IHttpForwarder
{
    Task<ForwarderError> SendAsync(HttpContext context, DestinationState selectedDestination, ClusterConfig cluster, IHttpTransformer transformer);
}

public class HttpForwarder : IHttpForwarder
{
    public async Task<ForwarderError> SendAsync(HttpContext context, DestinationState selectedDestination, ClusterConfig cluster, IHttpTransformer transformer)
    {
        //todo
        //try
        //{
        //    selectedDestination.ConcurrencyCounter.Increment();

        //    selectedDestination.ReportSuccessed();
        //}
        //catch
        //{
        //    selectedDestination.ReportFailed();
        //    throw;
        //}
        //finally
        //{
        //    selectedDestination.ConcurrencyCounter.Decrement();
        //}
        return ForwarderError.None;
    }
}
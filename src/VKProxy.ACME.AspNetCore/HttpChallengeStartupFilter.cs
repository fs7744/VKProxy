using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace VKProxy.ACME.AspNetCore;

internal class HttpChallengeStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.UseHttpChallengeResponseMiddleware();
            next(app);
        };
    }
}
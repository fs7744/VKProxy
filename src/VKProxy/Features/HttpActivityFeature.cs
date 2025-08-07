using Microsoft.AspNetCore.Http.Features;
using System.Diagnostics;

namespace VKProxy.Features;

public sealed class HttpActivityFeature : IHttpActivityFeature
{
    internal HttpActivityFeature(Activity activity)
    {
        Activity = activity;
    }

    /// <inheritdoc />
    public Activity Activity { get; set; }
}
﻿using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VKProxy.Core.Extensions;

public static class KestrelExtensions
{
    internal static IServiceCollection UseInternalKestrel(this IServiceCollection services, Action<KestrelServerOptions> options = null)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>());
        services.Configure<KestrelServerOptions>(o =>
        {
            options?.Invoke(o);
            o.AddServerHeader = false;
        });
        services.AddTransient<IConfigureOptions<KestrelServerOptions>, KestrelServerOptionsSetup>();
        services.AddTransient<KestrelServer>();
        return services;
    }
}

internal sealed class KestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
{
    private readonly IServiceProvider _services;

    public KestrelServerOptionsSetup(IServiceProvider services)
    {
        _services = services;
    }

    public void Configure(KestrelServerOptions options)
    {
        options.ApplicationServices = _services;
    }
}
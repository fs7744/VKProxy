using DotNext.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using VKProxy.Config;

namespace VKProxy.Middlewares.Http.HttpFuncs;

public class ResponseCompressionFunc : IHttpFunc
{
    private readonly IServiceProvider serviceProvider;

    public int Order => 20;

    public ResponseCompressionFunc(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public RequestDelegate Create(RouteConfig config, RequestDelegate next)
    {
        var cc = GetConfig(config);
        if (cc == null)
            return next;
        else
            return new ResponseCompressionMiddleware(next, cc).Invoke;
    }

    private IResponseCompressionProvider GetConfig(RouteConfig config)
    {
        var m = config.Metadata;
        if (m == null || !m.TryGetValue("ResponseCompression", out var v) || !bool.TryParse(v, out var rc) || !rc) return null;
        var options = new ResponseCompressionOptions() { EnableForHttps = false, MimeTypes = ResponseCompressionDefaults.MimeTypes };
        if (m.TryGetValue("ResponseCompressionMimeTypes", out v) && !string.IsNullOrWhiteSpace(v))
        {
            options.MimeTypes = v.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        if (m.TryGetValue("ResponseCompressionExcludedMimeTypes", out v) && !string.IsNullOrWhiteSpace(v))
        {
            options.ExcludedMimeTypes = v.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        if (m.TryGetValue("ResponseCompressionEnableForHttps", out v) && bool.TryParse(v, out rc))
        {
            options.EnableForHttps = rc;
        }

        if (!m.TryGetValue("ResponseCompressionLevel", out v) || !Enum.TryParse<CompressionLevel>(v, out var level))
            level = CompressionLevel.Fastest;

        switch (level)
        {
            case CompressionLevel.Optimal:
                options.Providers.AddAll(compressionProviderOptimal);
                break;

            case CompressionLevel.NoCompression:
                options.Providers.AddAll(compressionProviderNoCompression);
                break;

            case CompressionLevel.SmallestSize:
                options.Providers.AddAll(compressionProviderSmallestSize);
                break;

            case CompressionLevel.Fastest:
            default:
                options.Providers.AddAll(compressionProviderFastest);
                break;
        }

        return new ResponseCompressionProvider(serviceProvider, Options.Create<ResponseCompressionOptions>(options));
    }

    private static readonly ICompressionProvider[] compressionProviderOptimal = new ICompressionProvider[]
    {
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.Optimal }),
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.Optimal })
    };

    private static readonly ICompressionProvider[] compressionProviderNoCompression = new ICompressionProvider[]
    {
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.NoCompression }),
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.NoCompression })
    };

    private static readonly ICompressionProvider[] compressionProviderSmallestSize = new ICompressionProvider[]
    {
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.SmallestSize }),
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.SmallestSize })
    };

    private static readonly ICompressionProvider[] compressionProviderFastest = new ICompressionProvider[]
    {
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.Fastest }),
        new BrotliCompressionProvider(new BrotliCompressionProviderOptions() { Level = CompressionLevel.Fastest })
    };
}
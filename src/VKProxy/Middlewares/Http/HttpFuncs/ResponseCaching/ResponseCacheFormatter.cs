using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using VKProxy.Core.Infrastructure.Buffers;

namespace VKProxy.Middlewares.Http.HttpFuncs.ResponseCaching;

public static class ResponseCacheFormatter
{
    private static readonly string[] CommonHeaders = new string[]
{
        // DO NOT remove values, and do not re-order/insert - append only
        // NOTE: arbitrary common strings are fine - it doesn't all have to be headers
        HeaderNames.Accept,
        HeaderNames.AcceptCharset,
        HeaderNames.AcceptEncoding,
        HeaderNames.AcceptLanguage,
        HeaderNames.AcceptRanges,
        HeaderNames.AccessControlAllowCredentials,
        HeaderNames.AccessControlAllowHeaders,
        HeaderNames.AccessControlAllowMethods,
        HeaderNames.AccessControlAllowOrigin,
        HeaderNames.AccessControlExposeHeaders,
        HeaderNames.AccessControlMaxAge,
        HeaderNames.AccessControlRequestHeaders,
        HeaderNames.AccessControlRequestMethod,
        HeaderNames.Age,
        HeaderNames.Allow,
        HeaderNames.AltSvc,
        HeaderNames.Authorization,
        HeaderNames.Baggage,
        HeaderNames.CacheControl,
        HeaderNames.Connection,
        HeaderNames.ContentDisposition,
        HeaderNames.ContentEncoding,
        HeaderNames.ContentLanguage,
        HeaderNames.ContentLength,
        HeaderNames.ContentLocation,
        HeaderNames.ContentMD5,
        HeaderNames.ContentRange,
        HeaderNames.ContentSecurityPolicy,
        HeaderNames.ContentSecurityPolicyReportOnly,
        HeaderNames.ContentType,
        HeaderNames.CorrelationContext,
        HeaderNames.Cookie,
        HeaderNames.Date,
        HeaderNames.DNT,
        HeaderNames.ETag,
        HeaderNames.Expires,
        HeaderNames.Expect,
        HeaderNames.From,
        HeaderNames.Host,
        HeaderNames.KeepAlive,
        HeaderNames.IfMatch,
        HeaderNames.IfModifiedSince,
        HeaderNames.IfNoneMatch,
        HeaderNames.IfRange,
        HeaderNames.IfUnmodifiedSince,
        HeaderNames.LastModified,
        HeaderNames.Link,
        HeaderNames.Location,
        HeaderNames.MaxForwards,
        HeaderNames.Origin,
        HeaderNames.Pragma,
        HeaderNames.ProxyAuthenticate,
        HeaderNames.ProxyAuthorization,
        HeaderNames.ProxyConnection,
        HeaderNames.Range,
        HeaderNames.Referer,
        HeaderNames.RequestId,
        HeaderNames.RetryAfter,
        HeaderNames.Server,
        HeaderNames.StrictTransportSecurity,
        HeaderNames.TE,
        HeaderNames.Trailer,
        HeaderNames.TransferEncoding,
        HeaderNames.Translate,
        HeaderNames.TraceParent,
        HeaderNames.TraceState,
        HeaderNames.Vary,
        HeaderNames.Via,
        HeaderNames.Warning,
        HeaderNames.XContentTypeOptions,
        HeaderNames.XFrameOptions,
        HeaderNames.XPoweredBy,
        HeaderNames.XRequestedWith,
        HeaderNames.XUACompatible,
        HeaderNames.XXSSProtection,
        // additional MSFT headers
        "X-Rtag",
        "X-Vhost",

        // for Content-Type
        "text/html",
        "text/html; charset=utf-8",
        "text/html;charset=utf-8",
        "text/xml",
        "text/json",
        "application/x-binary",
        "image/svg+xml",
        "image/x-png",
        // for Accept-Encoding
        "gzip",
        "compress",
        "deflate",
        "br",
        "identity",
        "*",
        // for X-Frame-Options
        "SAMEORIGIN",
        "DENY",
        // for X-Content-Type
        "nosniff"

    // if you add new options here, you should rev the api version
};

    private static readonly FrozenSet<string> IgnoredHeaders = FrozenSet.ToFrozenSet(new[] {
            HeaderNames.RequestId, HeaderNames.ContentLength, HeaderNames.Age
    }, StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, int> CommonHeadersLookup = BuildCommonHeadersLookup();

    private static FrozenDictionary<string, int> BuildCommonHeadersLookup()
    {
        var arr = CommonHeaders;
        var pairs = new List<KeyValuePair<string, int>>(arr.Length);
        for (var i = 0; i < arr.Length; i++)
        {
            var header = arr[i];
            if (!string.IsNullOrWhiteSpace(header)) // omit null/empty values
            {
                pairs.Add(new(header, i));
            }
        }

        return FrozenDictionary.ToFrozenDictionary(pairs, StringComparer.OrdinalIgnoreCase);
    }

    internal static bool ShouldStoreHeader(string key) => !IgnoredHeaders.Contains(key);

    public static long EstimateCachedResponseSize(CachedResponse cachedResponse)
    {
        if (cachedResponse == null)
        {
            return 0L;
        }

        checked
        {
            // StatusCode
            long size = sizeof(int);

            // Headers
            if (cachedResponse.Headers != null)
            {
                foreach (var item in cachedResponse.Headers)
                {
                    size += (item.Key.Length * sizeof(char)) + EstimateStringValuesSize(item.Value);
                }
            }

            // Body
            if (cachedResponse.Body != null)
            {
                size += cachedResponse.Body.Length;
            }

            return size;
        }
    }

    internal static long EstimateStringValuesSize(StringValues stringValues)
    {
        checked
        {
            var size = 0L;

            for (var i = 0; i < stringValues.Count; i++)
            {
                var stringValue = stringValues[i];
                if (!string.IsNullOrEmpty(stringValue))
                {
                    size += stringValue.Length * sizeof(char);
                }
            }

            return size;
        }
    }

    // Format:
    // Creation date:
    //   Ticks: 7-bit encoded long
    //   Offset.TotalMinutes: 7-bit encoded long
    // Status code:
    //   7-bit encoded int
    // Headers:
    //   Headers count: 7-bit encoded int
    //   For each header:
    //     key name byte length: 7-bit encoded int
    //     UTF-8 encoded key name byte[]
    //     Values count: 7-bit encoded int
    //     For each header value:
    //       data byte length: 7-bit encoded int
    //       UTF-8 encoded byte[]
    // Body:
    //   Segments count: 7-bit encoded int
    //   For each segment:
    //     data byte length: 7-bit encoded int
    //     data byte[]
    // Tags:
    //   Tags count: 7-bit encoded int
    //   For each tag:
    //     data byte length: 7-bit encoded int
    //     UTF-8 encoded byte[]

    public static void Serialize(IBufferWriter<byte> output, CachedResponse entry)
    {
        var writer = new FormatterBinaryWriter(output);

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        writer.Write7BitEncodedInt64(entry.Created.Ticks);
        writer.Write7BitEncodedInt64((long)entry.Created.Offset.TotalMinutes);

        // Status code:
        //   7-bit encoded int
        writer.Write7BitEncodedInt(entry.StatusCode);

        // Headers:
        //   Headers count: 7-bit encoded int

        writer.Write7BitEncodedInt(entry.Headers.Count);

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]

        foreach (var header in entry.Headers)
        {
            WriteCommonHeader(ref writer, header.Key);

            //     Values count: 7-bit encoded int
            var count = header.Value.Count;
            writer.Write7BitEncodedInt(count);

            //     For each header value:
            //       data byte length: 7-bit encoded int
            //       UTF-8 encoded byte[]
            for (var i = 0; i < count; i++)
            {
                WriteCommonHeader(ref writer, header.Value[i]);
            }
        }

        // Body:
        //   Bytes count: 7-bit encoded int
        //     data byte[]
        if (entry.Body is CachedResponseBody body)
        {
            if (body == null || body.Segments == null || body.Segments.Count == 0)
            {
                writer.Write((byte)0);
            }
            else if (body.Segments.Count == 1)
            {
                var span = body.Segments.First();
                writer.Write7BitEncodedInt(span.Length);
                writer.WriteRaw(span);
            }
            else
            {
                writer.Write7BitEncodedInt(checked((int)body.Length));
                foreach (var segment in body.Segments)
                {
                    writer.WriteRaw(segment);
                }
            }
        }
        else if (entry.Body is CachedStreamResponseBody b)
        {
            writer.Write7BitEncodedInt(checked((int)b.Length));
            var reader = new BinaryReader(b.Stream);
            writer.WriteRaw(reader.ReadBytes(checked((int)b.Length)));
        }

        writer.Flush();
    }

    public static async Task SerializeAsync(Stream output, CachedResponse entry, CancellationToken cancellationToken)
    {
        var writer = new BinaryWriter(output);

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        writer.Write7BitEncodedInt64(entry.Created.Ticks);
        writer.Write7BitEncodedInt64((long)entry.Created.Offset.TotalMinutes);

        // Status code:
        //   7-bit encoded int
        writer.Write7BitEncodedInt(entry.StatusCode);

        // Headers:
        //   Headers count: 7-bit encoded int

        writer.Write7BitEncodedInt(entry.Headers.Count);

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]

        foreach (var header in entry.Headers)
        {
            WriteCommonHeader(writer, header.Key);

            //     Values count: 7-bit encoded int
            var count = header.Value.Count;
            writer.Write7BitEncodedInt(count);

            //     For each header value:
            //       data byte length: 7-bit encoded int
            //       UTF-8 encoded byte[]
            for (var i = 0; i < count; i++)
            {
                WriteCommonHeader(writer, header.Value[i]);
            }
        }

        // Body:
        //   Bytes count: 7-bit encoded int
        //     data byte[]

        if (entry.Body is CachedResponseBody body)
        {
            if (body == null || body.Segments == null || body.Segments.Count == 0)
            {
                writer.Write((byte)0);
            }
            else if (body.Segments.Count == 1)
            {
                var span = body.Segments.First();
                writer.Write7BitEncodedInt(span.Length);
                writer.Write(span);
            }
            else
            {
                writer.Write7BitEncodedInt(checked((int)body.Length));
                foreach (var segment in body.Segments)
                {
                    writer.Write(segment);
                }
            }
        }
        else if (entry.Body is CachedStreamResponseBody b)
        {
            writer.Write7BitEncodedInt(checked((int)b.Length));
            await b.Stream.CopyToAsync(output, cancellationToken);
        }

        writer.Flush();
    }

    private static void WriteCommonHeader(BinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write((byte)0);
        }
        else
        {
            if (CommonHeadersLookup.TryGetValue(value, out int known))
            {
                writer.Write7BitEncodedInt((known << 1) | 1);
            }
            else
            {
                if (value.Length == 0)
                {
                    writer.Write(0); // length prefix
                    return;
                }
                var bytes = Encoding.UTF8.GetBytes(value);
                writer.Write7BitEncodedInt(bytes.Length << 1); // length prefix
                writer.BaseStream.Write(bytes, 0, bytes.Length);
            }
        }
    }

    private static void WriteCommonHeader(ref FormatterBinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write((byte)0);
        }
        else
        {
            if (CommonHeadersLookup.TryGetValue(value, out int known))
            {
                writer.Write7BitEncodedInt((known << 1) | 1);
            }
            else
            {
                // use the length-prefixed UTF8 write in FormatterBinaryWriter,
                // but with a left-shift applied
                writer.Write(value, lengthShift: 1);
            }
        }
    }

    private static string ReadCommonHeader(ref FormatterBinaryReader reader)
    {
        int preamble = reader.Read7BitEncodedInt();
        // LSB means "using common header/value"
        if ((preamble & 1) == 1)
        {
            // non-LSB is the index of the common header
            return CommonHeaders[preamble >> 1];
        }
        else
        {
            // non-LSB is the string length
            return reader.ReadString(preamble >> 1);
        }
    }

    private static string ReadCommonHeader(BinaryReader reader)
    {
        int preamble = reader.Read7BitEncodedInt();
        // LSB means "using common header/value"
        if ((preamble & 1) == 1)
        {
            // non-LSB is the index of the common header
            return CommonHeaders[preamble >> 1];
        }
        else
        {
            return new string(reader.ReadChars(preamble >> 1));
        }
    }

    public static CachedResponse Deserialize(Stream content)
    {
        var reader = new BinaryReader(content);

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        var ticks = reader.Read7BitEncodedInt64();
        var offsetMinutes = reader.Read7BitEncodedInt64();

        var created = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));

        // Status code:
        //   7-bit encoded int

        var statusCode = reader.Read7BitEncodedInt();

        var result = new CachedResponse() { Created = created, StatusCode = statusCode };

        // Headers:
        //   Headers count: 7-bit encoded int

        var headersCount = reader.Read7BitEncodedInt();

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]
        //     Values count: 7-bit encoded int
        if (headersCount > 0)
        {
            var headers = result.Headers = new HeaderDictionary(headersCount);

            for (var i = 0; i < headersCount; i++)
            {
                var key = ReadCommonHeader(reader);
                StringValues value;
                var valuesCount = reader.Read7BitEncodedInt();
                //     For each header value:
                //       data byte length: 7-bit encoded int
                //       UTF-8 encoded byte[]
                switch (valuesCount)
                {
                    case < 0:
                        throw new InvalidOperationException();
                    case 0:
                        value = StringValues.Empty;
                        break;

                    case 1:
                        value = new(ReadCommonHeader(reader));
                        break;

                    default:
                        var values = new string[valuesCount];

                        for (var j = 0; j < valuesCount; j++)
                        {
                            values[j] = ReadCommonHeader(reader);
                        }
                        value = new(values);
                        break;
                }
                headers[key] = value;
            }
        }

        // Body:
        //   Bytes count: 7-bit encoded int

        var payloadLength = checked((int)reader.Read7BitEncodedInt64());
        if (payloadLength != 0)
        {   // since the reader only supports linear memory currently, read the entire chunk as a single piece
            result.Body = new CachedStreamResponseBody(content, payloadLength);
        }
        else
            result.Body = CachedResponseBody.Empty;
        return result;
    }

    public static CachedResponse Deserialize(ReadOnlyMemory<byte> content)
    {
        var reader = new FormatterBinaryReader(content);

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        var ticks = reader.Read7BitEncodedInt64();
        var offsetMinutes = reader.Read7BitEncodedInt64();

        var created = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));

        // Status code:
        //   7-bit encoded int

        var statusCode = reader.Read7BitEncodedInt();

        var result = new CachedResponse() { Created = created, StatusCode = statusCode };

        // Headers:
        //   Headers count: 7-bit encoded int

        var headersCount = reader.Read7BitEncodedInt();

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]
        //     Values count: 7-bit encoded int
        if (headersCount > 0)
        {
            var headers = result.Headers = new HeaderDictionary(headersCount);

            for (var i = 0; i < headersCount; i++)
            {
                var key = ReadCommonHeader(ref reader);
                StringValues value;
                var valuesCount = reader.Read7BitEncodedInt();
                //     For each header value:
                //       data byte length: 7-bit encoded int
                //       UTF-8 encoded byte[]
                switch (valuesCount)
                {
                    case < 0:
                        throw new InvalidOperationException();
                    case 0:
                        value = StringValues.Empty;
                        break;

                    case 1:
                        value = new(ReadCommonHeader(ref reader));
                        break;

                    default:
                        var values = new string[valuesCount];

                        for (var j = 0; j < valuesCount; j++)
                        {
                            values[j] = ReadCommonHeader(ref reader);
                        }
                        value = new(values);
                        break;
                }
                headers[key] = value;
            }
        }

        // Body:
        //   Bytes count: 7-bit encoded int

        var payloadLength = checked((int)reader.Read7BitEncodedInt64());
        if (payloadLength != 0)
        {   // since the reader only supports linear memory currently, read the entire chunk as a single piece
            result.Body = new CachedResponseBody(new List<byte[]>(1) { reader.ReadBytesSpan(payloadLength).ToArray() }, payloadLength);
        }
        else
            result.Body = CachedResponseBody.Empty;
        Debug.Assert(reader.IsEOF, "should have read entire payload");
        return result;
    }
}
using VKProxy.Core.Infrastructure;

namespace VKProxy.Middlewares.Http.Transforms;

internal class ForwardedTransformFactory : ITransformFactory
{
    internal const string XForwardedKey = "X-Forwarded";
    internal const string DefaultXForwardedPrefix = "X-Forwarded-";
    internal const string ForwardedKey = "Forwarded";
    internal const string ActionKey = "Action";
    internal const string HeaderPrefixKey = "HeaderPrefix";
    internal const string ForKey = "For";
    internal const string ByKey = "By";
    internal const string HostKey = "Host";
    internal const string ProtoKey = "Proto";
    internal const string PrefixKey = "Prefix";
    internal const string ForFormatKey = "ForFormat";
    internal const string ByFormatKey = "ByFormat";
    internal const string ClientCertKey = "ClientCert";

    private readonly IRandomFactory _randomFactory;

    public ForwardedTransformFactory(IRandomFactory randomFactory)
    {
        _randomFactory = randomFactory;
    }

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        var r = false;
        if (transformValues.TryGetValue(XForwardedKey, out var headerValue))
        {
            var xExpected = 1;
            ValidateAction(context, XForwardedKey, headerValue, out var defaultXAction);

            var prefix = DefaultXForwardedPrefix;
            if (transformValues.TryGetValue(HeaderPrefixKey, out var prefixValue))
            {
                xExpected++;
                prefix = prefixValue;
            }

            var xForAction = defaultXAction;
            if (transformValues.TryGetValue(ForKey, out headerValue)
                && ValidateAction(context, ForKey, headerValue, out var xxForAction))
            {
                xExpected++;
                xForAction = xxForAction;
            }

            var xPrefixAction = defaultXAction;
            if (transformValues.TryGetValue(PrefixKey, out headerValue)
                && ValidateAction(context, PrefixKey, headerValue, out var xxPrefixAction))
            {
                xExpected++;
                xPrefixAction = xxPrefixAction;
            }

            var xHostAction = defaultXAction;
            if (transformValues.TryGetValue(HostKey, out headerValue)
                && ValidateAction(context, HostKey, headerValue, out var xxHostAction))
            {
                xExpected++;
                xHostAction = xxHostAction;
            }

            var xProtoAction = defaultXAction;
            if (transformValues.TryGetValue(ProtoKey, out headerValue)
                && ValidateAction(context, ProtoKey, headerValue, out var xxProtoAction))
            {
                xExpected++;
                xProtoAction = xxProtoAction;
            }

            if (TransformHelpers.CheckTooManyParameters(context, transformValues, xExpected))
            {
                context.AddXForwardedFor(prefix + ForKey, xForAction);
                context.AddXForwardedPrefix(prefix + PrefixKey, xPrefixAction);
                context.AddXForwardedHost(prefix + HostKey, xHostAction);
                context.AddXForwardedProto(prefix + ProtoKey, xProtoAction);

                if (xForAction != ForwardedTransformActions.Off || xPrefixAction != ForwardedTransformActions.Off
                    || xHostAction != ForwardedTransformActions.Off || xProtoAction != ForwardedTransformActions.Off)
                {
                    // Remove the Forwarded header when an X-Forwarded transform is enabled
                    TransformHelpers.RemoveForwardedHeader(context);
                }
                r = true;
            }
        }
        else if (transformValues.TryGetValue(ForwardedKey, out var forwardedHeader))
        {
            var useHost = false;
            var useProto = false;
            var useFor = false;
            var useBy = false;
            var forFormat = NodeFormat.None;
            var byFormat = NodeFormat.None;

            // for, host, proto, Prefix
            var tokens = forwardedHeader.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                if (string.Equals(token, ForKey, StringComparison.OrdinalIgnoreCase))
                {
                    useFor = true;
                    forFormat = NodeFormat.Random; // RFC Default
                }
                else if (string.Equals(token, ByKey, StringComparison.OrdinalIgnoreCase))
                {
                    useBy = true;
                    byFormat = NodeFormat.Random; // RFC Default
                }
                else if (string.Equals(token, HostKey, StringComparison.OrdinalIgnoreCase))
                {
                    useHost = true;
                }
                else if (string.Equals(token, ProtoKey, StringComparison.OrdinalIgnoreCase))
                {
                    useProto = true;
                }
                else
                {
                    context.Errors.Add(new ArgumentException($"Unexpected value for Forwarded: {token}. Expected 'for', 'host', 'proto', or 'by'"));
                }
            }

            var expected = 1;

            var headerAction = ForwardedTransformActions.Set;
            if (transformValues.TryGetValue(ActionKey, out headerValue)
                && ValidateAction(context, ActionKey, headerValue, out var xheaderAction))
            {
                expected++;
                headerAction = xheaderAction;
            }

            if (useFor && transformValues.TryGetValue(ForFormatKey, out var forFormatString)
                && ValidateNodeFormat(context, ForFormatKey, forFormatString, out var xforFormatString))
            {
                expected++;
                forFormat = xforFormatString;
            }

            if (useBy && transformValues.TryGetValue(ByFormatKey, out var byFormatString)
                && ValidateNodeFormat(context, ByFormatKey, byFormatString, out var xbyFormatString))
            {
                expected++;
                byFormat = xbyFormatString;
            }

            if (TransformHelpers.CheckTooManyParameters(context, transformValues, expected))
            {
                context.UseDefaultForwarders = false;
                if (headerAction != ForwardedTransformActions.Off && (useBy || useFor || useHost || useProto))
                {
                    // Not using the extension to avoid resolving the random factory each time.
                    context.RequestTransforms.Add(new RequestHeaderForwardedTransform(_randomFactory, forFormat, byFormat, useHost, useProto, headerAction));

                    // Remove the X-Forwarded headers when a Forwarded transform is enabled
                    TransformHelpers.RemoveAllXForwardedHeaders(context, DefaultXForwardedPrefix);
                }
                r = true;
            }
        }
        else if (transformValues.TryGetValue(ClientCertKey, out var clientCertHeader))
        {
            if (TransformHelpers.CheckTooManyParameters(context, transformValues, expected: 1))
            {
                context.AddClientCertHeader(clientCertHeader);
                r = true;
            }
        }

        return r;
    }

    private static bool ValidateAction(TransformBuilderContext context, string key, string? headerValue, out ForwardedTransformActions actions)
    {
        if (!Enum.TryParse<ForwardedTransformActions>(headerValue, out actions))
        {
            context.Errors.Add(new ArgumentException($"Unexpected value for {key}: {headerValue}. Expected one of {nameof(ForwardedTransformActions)}"));
            return false;
        }
        return true;
    }

    private static bool ValidateNodeFormat(TransformBuilderContext context, string key, string? forFormat, out NodeFormat enumValues)
    {
        if (!Enum.TryParse<NodeFormat>(forFormat, ignoreCase: true, out enumValues))
        {
            context.Errors.Add(new ArgumentException($"Unexpected value for Forwarded:ForFormat: {forFormat}. Expected: {enumValues}"));
            return false;
        }
        return true;
    }
}
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Authentication;

namespace VKProxy.Core.Config;

public static class ConfigurationReadingExtensions
{
    public static int? ReadInt32(this IConfiguration configuration, string name)
    {
        return configuration[name] is string value ? int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) : null;
    }

    public static long? ReadInt64(this IConfiguration configuration, string name, long? defaultValue = null)
    {
        return configuration[name] is string value ? long.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) : defaultValue;
    }

    public static double? ReadDouble(this IConfiguration configuration, string name)
    {
        return configuration[name] is string value ? double.Parse(value, CultureInfo.InvariantCulture) : null;
    }

    public static TimeSpan? ReadTimeSpan(this IConfiguration configuration, string name)
    {
        // Format "c" => [-][d'.']hh':'mm':'ss['.'fffffff].
        // You also can find more info at https://docs.microsoft.com/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier
        return configuration[name] is string value ? TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture) : null;
    }

    public static Uri? ReadUri(this IConfiguration configuration, string name)
    {
        return configuration[name] is string value ? new Uri(value) : null;
    }

    public static TEnum? ReadEnum<TEnum>(this IConfiguration configuration, string name) where TEnum : struct
    {
        return configuration[name] is string value ? Enum.Parse<TEnum>(value, ignoreCase: true) : null;
    }

    public static SslProtocols? ReadSslProtocols(this IConfiguration configuration, string name)
    {
        if (configuration[name] is string value)
        {
            return Enum.Parse<SslProtocols>(value, ignoreCase: true);
        }
        else
        {
            var s = configuration.GetSection(name);
            if (!s.Exists() || (s.GetChildren() is var children && !children.Any())) return null;
            return s.GetChildren().Select(i => i.Value).Where(i => i != null).Select(i => Enum.Parse<SslProtocols>(i, ignoreCase: true))
                .Aggregate((i, j) => i | j);
        }
    }

    public static bool? ReadBool(this IConfiguration configuration, string name)
    {
        return configuration[name] is string value ? bool.Parse(value) : null;
    }

    public static Version? ReadVersion(this IConfiguration configuration, string name)
    {
        return configuration[name] is string value && !string.IsNullOrEmpty(value) ? Version.Parse(value + (value.Contains('.') ? "" : ".0")) : null;
    }

    public static IReadOnlyDictionary<string, string>? ReadStringDictionary(this IConfigurationSection section)
    {
        if (section.GetChildren() is var children && !children.Any())
        {
            return null;
        }

        return new ReadOnlyDictionary<string, string>(children.ToDictionary(s => s.Key, s => s.Value!, StringComparer.OrdinalIgnoreCase));
    }

    public static string[]? ReadStringArray(this IConfigurationSection section)
    {
        if (section.GetChildren() is var children && !children.Any())
        {
            return null;
        }

        return children.Select(s => s.Value!).ToArray();
    }
}
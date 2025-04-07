using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace VKProxy.Core.Config;

public class NothingConfigurationSection : IConfigurationSection
{
    public static readonly NothingConfigurationSection Nothing = new NothingConfigurationSection();

    public string? this[string key]
    { get => null; set { } }

    public string Key => null;

    public string Path => null;

    public string? Value
    { get => null; set { } }

    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return Enumerable.Empty<IConfigurationSection>();
    }

    public IChangeToken GetReloadToken()
    {
        return null;
    }

    public IConfigurationSection GetSection(string key)
    {
        return null;
    }
}
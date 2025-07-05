using Microsoft.AspNetCore.DataProtection.Repositories;
using StackExchange.Redis;
using System.Xml.Linq;

namespace VKProxy.StackExchangeRedis;

public class RedisXmlRepository : IXmlRepository
{
    private readonly IRedisPool pool;
    private readonly RedisKey key;

    public RedisXmlRepository(IRedisPool pool, RedisKey key)
    {
        this.pool = pool;
        this.key = key;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        return GetAllElementsCore().ToList().AsReadOnly();
    }

    private IEnumerable<XElement> GetAllElementsCore()
    {
        using var r = pool.Rent();
        var database = r.Obj.GetDatabase();
        foreach (var value in database.ListRange(key))
        {
            yield return XElement.Parse((string)value!);
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var r = pool.Rent();
        var database = r.Obj.GetDatabase();
        database.ListRightPush(key, element.ToString(SaveOptions.DisableFormatting));
    }
}
namespace VKProxy.Core.Infrastructure;

public static class AddressParser
{
    public static (string host, int port) Parse(string address)
    {
        string hostName;
        int port;
        if (address.Contains("://"))
        {
            var originalUri = new Uri(address);
            //var originalHost = item.Host is { Length: > 0 } host ? host : originalUri.Authority;
            hostName = originalUri.DnsSafeHost;
            port = originalUri.Port;
        }
        else
        {
            var i = address.IndexOf(':');
            if (i != -1 && int.TryParse(address.AsSpan(i + 1), out var p))
            {
                hostName = address[..i];
                port = p;
            }
            else
            {
                hostName = address;
                port = 80;
            }
        }

        return (hostName, port);
    }
}
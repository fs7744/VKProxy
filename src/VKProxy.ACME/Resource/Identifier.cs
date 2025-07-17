namespace VKProxy.ACME.Resource;

public class Identifier
{
    public IdentifierType Type { get; set; }

    public string Value { get; set; }
}

public enum IdentifierType
{
    Dns,
}
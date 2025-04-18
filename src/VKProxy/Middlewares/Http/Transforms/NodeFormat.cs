namespace VKProxy.Middlewares.Http.Transforms;

public enum NodeFormat
{
    None,
    Random,
    RandomAndPort,
    RandomAndRandomPort,
    Unknown,
    UnknownAndPort,
    UnknownAndRandomPort,
    Ip,
    IpAndPort,
    IpAndRandomPort,
}
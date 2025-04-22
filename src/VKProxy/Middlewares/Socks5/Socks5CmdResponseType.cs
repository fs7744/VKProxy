namespace VKProxy.Middlewares.Socks5;

public enum Socks5CmdResponseType : byte
{
    Success = 0,
    ServerError = 1,
    ConnectNotAllow = 2,
    NetworkError = 3,
    ConnectFail = 4,
    DistReject = 5,
    TTLTimeout = 6,
    CommandNotAllow = 7,
    AddressNotAllow = 8,
    Unknow = 8,
}
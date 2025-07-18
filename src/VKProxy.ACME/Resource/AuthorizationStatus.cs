namespace VKProxy.ACME.Resource;

public enum AuthorizationStatus
{
    Pending,
    Valid,
    Invalid,
    Revoked,
    Deactivated,
    Expired,
}
namespace VKProxy.ACME.Resource;

public class Authorization
{
    public Identifier Identifier { get; set; }
    public AuthorizationStatus? Status { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public Uri Scope { get; set; }
    public IList<Challenge> Challenges { get; set; }
    public bool? Wildcard { get; set; }
}
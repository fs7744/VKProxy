namespace VKProxy.ACME.Resource;

public class AcmeDirectoryMeta
{
    public Uri TermsOfService { get; set; }
    public Uri Website { get; set; }
    public IList<string> CaaIdentities { get; set; }
    public bool? ExternalAccountRequired { get; set; }
}
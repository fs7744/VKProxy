using System.Text.Json.Serialization;

namespace VKProxy.ACME.Resource;

public class AcmeDirectory
{
    public Uri NewNonce { get; set; }
    public Uri NewAccount { get; set; }
    public Uri NewOrder { get; set; }
    public Uri RevokeCert { get; set; }
    public Uri KeyChange { get; set; }
    public Uri RenewalInfo { get; set; }
    public AcmeDirectoryMeta Meta { get; set; }
}
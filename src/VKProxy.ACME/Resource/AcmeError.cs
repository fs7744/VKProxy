using System.Net;

namespace VKProxy.ACME.Resource;

public class AcmeError
{
    public string Type { get; set; }
    public string Detail { get; set; }
    public Identifier Identifier { get; set; }
    public IList<AcmeError> Subproblems { get; set; }
    public HttpStatusCode Status { get; set; }
}
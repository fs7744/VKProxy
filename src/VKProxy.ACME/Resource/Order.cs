namespace VKProxy.ACME.Resource;

public class Order
{
    public OrderStatus? Status { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public IList<Identifier> Identifiers { get; set; }
    public DateTimeOffset? NotBefore { get; set; }
    public DateTimeOffset? NotAfter { get; set; }

    /// <summary>
    /// model https://tools.ietf.org/html/rfc7807
    /// </summary>
    public object Error { get; set; }

    public IList<Uri> Authorizations { get; set; }
    public Uri Finalize { get; set; }
    public Uri Certificate { get; set; }

    internal class Payload : Order
    {
        public string Csr { get; set; }
    }
}
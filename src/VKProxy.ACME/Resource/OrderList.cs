namespace VKProxy.ACME.Resource;

/// <summary>
/// Represents the ACME Orders List resource.
/// </summary>
/// <remarks>
/// As https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-7.1.2.1
/// </remarks>
public class OrderList
{
    public IList<Uri> Orders { get; set; }
}
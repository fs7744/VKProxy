namespace VKProxy.ACME;

public class CsrInfo
{
    public string CountryName { get; set; }
    public string State { get; set; }
    public string Locality { get; set; }
    public string Organization { get; set; }
    public string OrganizationUnit { get; set; }
    public string CommonName { get; set; }

    public IEnumerable<KeyValuePair<string, string>> GetFields()
    {
        if (CountryName != null)
            yield return new KeyValuePair<string, string>("C", CountryName);
        if (State != null)
            yield return new KeyValuePair<string, string>("ST", State);
        if (Locality != null)
            yield return new KeyValuePair<string, string>("L", Locality);
        if (Organization != null)
            yield return new KeyValuePair<string, string>("O", Organization);
        if (OrganizationUnit != null)
            yield return new KeyValuePair<string, string>("OU", OrganizationUnit);
        if (CommonName != null)
            yield return new KeyValuePair<string, string>("CN", CommonName);
    }
}
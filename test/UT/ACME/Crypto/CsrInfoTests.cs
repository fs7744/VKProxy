using VKProxy.ACME;

namespace UT.ACME.Crypto;

public class CsrInfoTests
{
    [Fact]
    public void CanSetInfo()
    {
        var csr = new CsrInfo
        {
            CommonName = "CommonName",
            Locality = "Locality",
            CountryName = "CountryName",
            Organization = "Organization",
            OrganizationUnit = "OrganizationUnit",
            State = "State",
        };

        Assert.Equal("CommonName", csr.GetFields().Single(f => f.Key == "CN").Value);
        Assert.Equal("Locality", csr.GetFields().Single(f => f.Key == "L").Value);
        Assert.Equal("CountryName", csr.GetFields().Single(f => f.Key == "C").Value);
        Assert.Equal("Organization", csr.GetFields().Single(f => f.Key == "O").Value);
        Assert.Equal("OrganizationUnit", csr.GetFields().Single(f => f.Key == "OU").Value);
        Assert.Equal("State", csr.GetFields().Single(f => f.Key == "ST").Value);
    }
}
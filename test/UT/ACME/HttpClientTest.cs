using DotNext.Collections.Generic;
using VKProxy.ACME;

namespace UT.ACME;

public class HttpClientTest
{
    [Theory]
    [InlineData("<https://example.com/acme/directory>;rel=\"index\"")]
    [InlineData("<https://example.com/acme/directory>;rel=\"index\",<https://example.com/acme/terms/2017-6-02>;rel=\"terms-of-service\"")]
    public void ExtractLinksFromResponseTest(string data)
    {
        var resp = new HttpResponseMessage();
        resp.Headers.TryAddWithoutValidation("Link", data.Split(','));
        DefaultAcmeHttpClient.ExtractLinksFromResponse(resp).ForEach(i =>
        {
            Assert.False(i.Key.Contains('"'));
            Assert.False(i.Key.Contains("rel="));
            Assert.Single(i);
        });
    }
}
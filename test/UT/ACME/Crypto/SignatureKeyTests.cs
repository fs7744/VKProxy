using Newtonsoft.Json;
using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace UT.ACME.Crypto;

public class SignatureKeyTests
{
    [Theory]
    [InlineData(KeyAlgorithm.RS256)]
    [InlineData(KeyAlgorithm.ES256)]
    [InlineData(KeyAlgorithm.ES384)]
    [InlineData(KeyAlgorithm.ES512)]
    public void CanExportKey(KeyAlgorithm signatureAlgorithm)
    {
        var algo = KeyAlgorithmProvider.Get(signatureAlgorithm);
        var key = algo.GenerateKey();
        Assert.NotNull(key);

        var der = key.ToDer();
        var exported = KeyAlgorithmProvider.GetKey(der);

        Assert.Equal(
            JsonConvert.SerializeObject(key.JsonWebKey),
            JsonConvert.SerializeObject(exported.JsonWebKey));

        var pem = key.ToPem();
        exported = KeyAlgorithmProvider.GetKey(pem);

        Assert.Equal(
            JsonConvert.SerializeObject(key.JsonWebKey),
            JsonConvert.SerializeObject(exported.JsonWebKey));
    }

    [Theory]
    [InlineData(Keys.ES256Key)]
    [InlineData(Keys.ES256Key_Alt1)]
    [InlineData(Keys.ES384Key)]
    [InlineData(Keys.ES512Key)]
    public void CanEncodeJsonWebKey(string key)
    {
        var k = KeyAlgorithmProvider.GetKey(key);
        var ecKey = (EcJsonWebKey)k.JsonWebKey;

        Assert.Equal("EC", ecKey.KeyType);
        Assert.Equal(ecKey.X.Length, ecKey.X.Length);
    }

    [Fact]
    public void CanPadECCoordBytes()
    {
        var k = KeyAlgorithmProvider.GetKey(Keys.ES256Key_Alt1);
        var ecKey = (EcJsonWebKey)k.JsonWebKey;

        Assert.Equal("AJz0yAAXAwEmOhTRkjXxwgedbWO6gobYM3lWszrS68E", ecKey.X);
        Assert.Equal("vEEs4V0egJkNyM2Q4pp001zu14VcpQ0_Ei8xOOPxKZs", ecKey.Y);

        k = KeyAlgorithmProvider.GetKey(Keys.ES256Key);
        ecKey = (EcJsonWebKey)k.JsonWebKey;

        Assert.Equal("dHVy6M_8l7UibLdFPlhnbdNv-LROnx6_FcdyFArBd_s", ecKey.X);
        Assert.Equal("2xBzsnlAASQN0jQYuxdWybSzEQtsxoT-z7XGIDp0k_c", ecKey.Y);
    }

    [Fact]
    public void EnsurePropertySerializationOrder()
    {
        /// https://tools.ietf.org/html/rfc7638#page-8
        /// The lexographical (alphabetical) order of the serialized key is important for JWK thumprint validation
        /// without a specified order this order can vary across platforms during property reflection.

        var k = KeyAlgorithmProvider.GetKey(Keys.ES256Key_Alt1);
        var ecKey = (EcJsonWebKey)k.JsonWebKey;
        var ecKeyJson = System.Text.Json.JsonSerializer.Serialize(ecKey, DefaultAcmeHttpClient.JsonSerializerOptions);

        Assert.Equal(
            "{\"crv\":\"P-256\",\"kty\":\"EC\",\"x\":\"AJz0yAAXAwEmOhTRkjXxwgedbWO6gobYM3lWszrS68E\",\"y\":\"vEEs4V0egJkNyM2Q4pp001zu14VcpQ0_Ei8xOOPxKZs\"}"
            , ecKeyJson
            );

        k = KeyAlgorithmProvider.GetKey(Keys.RS256Key);
        var rsaKey = (RsaJsonWebKey)k.JsonWebKey;
        var rsaKeyJson = System.Text.Json.JsonSerializer.Serialize(rsaKey, DefaultAcmeHttpClient.JsonSerializerOptions);

        Assert.Equal(
            "{\"e\":\"AQAB\",\"kty\":\"RSA\",\"n\":\"maeT6EsXTVHAdwuq3IlAl9uljXE5CnkRpr6uSw_Fk9nQshfZqKFdeZHkSBvIaLirE2ZidMEYy-rpS1O2j-viTG5U6bUSWo8aoeKoXwYfwbXNboEA-P4HgGCjD22XaXAkBHdhgyZ0UBX2z-jCx1smd7nucsi4h4RhC_2cEB1x_mE6XS5VlpvG91Hbcgml4cl0NZrWPtJ4DhFdPNUtQ8q3AYXkOr_OSFZgRKjesRaqfnSdJNABqlO_jEzAx0fgJfPZe1WlRWOfGRVBVopZ4_N5HpR_9lsNDzCZyidFsHwzvpkP6R6HbS8CMrNWgtkTbnz27EVqIhkYdiPVIN2Xkwj0BQ\"}"
            ,
            rsaKeyJson
            );
    }
}
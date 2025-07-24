using System.Net;
using System.Text;
using VKProxy;
using VKProxy.ACME.Crypto;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
//builder.Services.Configure<ReverseProxyOptions>(o => o.Section = "ReverseProxy2");
//builder.Services.UseReverseProxy().UseSocks5();

builder.Services.AddAcmeChallenge(o =>
{
    o.RenewDaysInAdvance = TimeSpan.FromDays(2);
    o.Server = new Uri("https://127.0.0.1:14000/dir");
    o.DomainNames = new[] { "kubernetes.docker.internal" };
    o.NewAccount(new string[] { "mailto:test@xxx.com" });
    o.AdditionalIssuers = new[] {"""
            -----BEGIN CERTIFICATE-----
            MIIDGzCCAgOgAwIBAgIIUPFry5qBu34wDQYJKoZIhvcNAQELBQAwIDEeMBwGA1UE
            AxMVUGViYmxlIFJvb3QgQ0EgMjFjNjY3MCAXDTI1MDcyMjAxMTA0OVoYDzIwNTUw
            NzIyMDExMDQ5WjAgMR4wHAYDVQQDExVQZWJibGUgUm9vdCBDQSAyMWM2NjcwggEi
            MA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQCxNKa4y93OFYaSx8bcbuWsHHnW
            mpfsobK5Elf7GE02mi/cDrMP+wR1l53BuucrW04OyoewkBsJNZoxEy1DkCjxv4+g
            Q+HgGCR5R14ex17ZdFxpcl42H8QnRB3IqVBlJiz0JyGZwiaOamOkUTVEYTGDeuxu
            PglpvboGeatsWQe0MJJfBN8OxLVUmi6Y/enbzlIdv3tvgQujfPNiS8MLDMBuIiMs
            ixhu8YAzUqvVKZoQVK7GwbD9WrVBKub8w86StKFmU14aSXahidt8IENdpLO2OT3J
            y1nt25QDsAmtS1/wGnTDPeefLGsM7kGYNesQkSW0w8Um4p9KLWKnKyOvzPZrAgMB
            AAGjVzBVMA4GA1UdDwEB/wQEAwIChDATBgNVHSUEDDAKBggrBgEFBQcDATAPBgNV
            HRMBAf8EBTADAQH/MB0GA1UdDgQWBBRoXcwo6c5J8jMweiHKPw4OlcWIQzANBgkq
            hkiG9w0BAQsFAAOCAQEAad9XT4sN1KserYtCxBKmoPhPAHInHYgG/Z2gd6KqdsK9
            biIgEbKo84tClLqA6XCN/yN1bMQL2ZMbWBF8oHv/A5o0atpTpd+Ho+punHYRIpqv
            akUX21Zsu6NdAuH7g7m9t9h/lc6tgiqaAf2HwpC3NrXmUlPRqLay7/t+BFQU6dBa
            E+qzmL7lHZQf1UArfb+QDYH2XsFCk9Pjv0xdP+PGwf8HqHhfPLctvus5JL+LXp0X
            68eWKQCs1CrL8cUMwcELlW/mR1lKnJL1WgM1Bns9ZF1ha6egG539ruzQjItF6MHB
            xAEt55nXfs+mjV1p7qrcmR8jIdByR9C36T21r+8pKA==
            -----END CERTIFICATE-----

            """
};
}, c =>
{
    c.HttpClientConfig = new VKProxy.Config.HttpClientConfig()
    {
        DangerousAcceptAnyServerCertificate = true
    };
});

builder.WebHost.UseKestrel(k =>
{
    k.ConfigureHttpsDefaults(i => i.UseAcmeChallenge(k.ApplicationServices));

    //k.Listen(IPAddress.Any, 443,
    //                        o =>
    //                            o.UseHttps(h => h.UseAcmeChallenge(k.ApplicationServices)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
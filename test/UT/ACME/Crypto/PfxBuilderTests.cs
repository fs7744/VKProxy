﻿using System.Text;
using VKProxy.ACME;
using VKProxy.ACME.Crypto;

namespace UT.ACME.Crypto;

public class PfxBuilderTests
{
    [Theory]
    [InlineData(KeyAlgorithm.RS256)]
    [InlineData(KeyAlgorithm.ES256)]
    [InlineData(KeyAlgorithm.ES384)]
    [InlineData(KeyAlgorithm.ES512)]
    public void CanCreatePfxWithoutChain(KeyAlgorithm alog)
    {
        var leafCert = """
            -----BEGIN CERTIFICATE-----
            MIIE4jCCA8qgAwIBAgITAP/yVp9G7TS9u2bIwkKIKzfKgDANBgkqhkiG9w0BAQsF
            ADAfMR0wGwYDVQQDDBRoMnBweSBoMmNrZXIgZmFrZSBDQTAeFw0xODAyMjMwNDM5
            MDdaFw0xODA1MjQwNDM5MDdaMGIxMTAvBgNVBAMTKHd3dy1odHRwLWRldi5lczI1
            Ni5jZXJ0ZXMtY2kuZHltZXRpcy5jb20xLTArBgNVBAUTJGZmZjI1NjlmNDZlZDM0
            YmRiYjY2YzhjMjQyODgyYjM3Y2E4MDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCC
            AQoCggEBAIJ2OOJMRerxaM9BZhE4S4STZEDOIELvhjLG9FaHh3eZVlsv7XY7ImtC
            Tm9/tN1E7SIPbjvXpo3K15227qhDPpyZ02gFGtteoeVBG+49NKyVfQPcIYYFT5lF
            m687zYGXShWU0UGeKaoohVv5VZ5kaQhbWzKzqFrsEdV5IClmlKf7NaqFUag7W/v6
            QGx4cLiygPceArMLclJRy5bJzEr+g5nWbRVTq0PbCIIHJlAPMu+RUoiiz8ChqnQ6
            csgUzSa8lBGE+Ex3ooZl8MoQRRLJz6rZZUf9aMYeJunMgLDV6NIu72E+JKweHRuD
            ePI+bb7KYVpX8NLdJdvzc9gnhs8gH8MCAwEAAaOCAdIwggHOMA4GA1UdDwEB/wQE
            AwIFoDAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwDAYDVR0TAQH/BAIw
            ADAdBgNVHQ4EFgQUU8q+UBchxCSpHJZ9v+5mLjPbzCEwHwYDVR0jBBgwFoAU+3hP
            EvlgFYMsnxd/NBmzLjbqQYkwYwYIKwYBBQUHAQEEVzBVMCIGCCsGAQUFBzABhhZo
            dHRwOi8vMTI3LjAuMC4xOjQwMDIvMC8GCCsGAQUFBzAChiNodHRwOi8vbG8wLmlu
            OjQ0MzAvYWNtZS9pc3N1ZXItY2VydDBeBgNVHREEVzBVgiltYWlsLWh0dHAtZGV2
            LmVzMjU2LmNlcnRlcy1jaS5keW1ldGlzLmNvbYIod3d3LWh0dHAtZGV2LmVzMjU2
            LmNlcnRlcy1jaS5keW1ldGlzLmNvbTAnBgNVHR8EIDAeMBygGqAYhhZodHRwOi8v
            ZXhhbXBsZS5jb20vY3JsMGEGA1UdIARaMFgwCAYGZ4EMAQIBMEwGAyoDBDBFMCIG
            CCsGAQUFBwIBFhZodHRwOi8vZXhhbXBsZS5jb20vY3BzMB8GCCsGAQUFBwICMBMM
            EURvIFdoYXQgVGhvdSBXaWx0MA0GCSqGSIb3DQEBCwUAA4IBAQBqaeedBQq59RTv
            XGrtAM59SKerNk7ff83bDY3QZHgGRWBadguDhQNskGZ7DKbskKYUCkohY95SuiE6
            le4tEKzP6PePRd6MZ8x33cXMUJqHDqCH6+QA46pBZJeDK5zt+MJbYuXN5sHPEr8L
            Zy5gd1gZ2T+Ue1UUmMLvBjALVGYi9EuElbuj8VYX+jdHzjwidvVgwyuyaWDkeGVJ
            Zeuvr+lIBE8q2+1H2/J36JQeZ2ns8k7+moF/CFKOLei7VPAuBcunlm2DZD2jvCDx
            Wu8TFOmaJVNb/tOo31PSSMFhdw0kV+Hh2EceYFG6JGjT9Y5+YLuqW0ymEmf2It6H
            +ToBL+6O
            -----END CERTIFICATE-----
            """;

        var pfxBuilder = new PfxBuilder(
            Encoding.UTF8.GetBytes(leafCert), alog.NewKey());
        pfxBuilder.FullChain = false;
        var pfx = pfxBuilder.Build("my-cert", "abcd1234");
    }
}
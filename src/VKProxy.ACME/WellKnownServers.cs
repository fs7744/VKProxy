namespace VKProxy.ACME;

public static class WellKnownServers
{
    public static readonly Uri LetsEncryptV2 = new Uri("https://acme-v02.api.letsencrypt.org/directory");
    public static readonly Uri LetsEncryptStagingV2 = new Uri("https://acme-staging-v02.api.letsencrypt.org/directory");
}
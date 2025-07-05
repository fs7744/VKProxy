using Microsoft.AspNetCore.Http;

namespace VKProxy.LoadBalancing.SessionAffinity;

public class SessionAffinityCookieOptions
{
    public CookieOptions Options { get; set; } = new CookieOptions();
    public string Name { get; set; }
    public TimeSpan? Expires { get; set; }

    public CookieOptions Create()
    {
        return Expires.HasValue ? new CookieOptions()
        {
            Path = Options.Path,
            SameSite = Options.SameSite,
            HttpOnly = Options.HttpOnly,
            MaxAge = Options.MaxAge,
            Domain = Options.Domain,
            IsEssential = Options.IsEssential,
            Secure = Options.Secure,
            Expires = Expires.HasValue ? DateTime.UtcNow.Add(Expires.Value) : default(DateTimeOffset?),
        }
        : Options;
    }
}
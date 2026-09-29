namespace Innovia.Api.Common.Auth.Cookies;

public static class CookieOptionsFactory
{
    public static CookieOptions CreateAccessTokenCookieOptions(DateTimeOffset expires, bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.None,
        Expires = expires
    };
    public static CookieOptions CreateRefreshTokenCookieOptions(DateTimeOffset expires, bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.None,
        Expires = expires,
        Path = "/auth"
    };
}

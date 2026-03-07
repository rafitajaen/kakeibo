using Microsoft.Extensions.Options;

namespace Kakeibo.Api.Infrastructure.Auth;

// Sets JWT access and refresh token HttpOnly cookies on the HTTP response.
public sealed class TokenCookieService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public void SetTokenCookies(HttpContext httpContext, string accessToken, string refreshToken)
    {
        var isSecure = httpContext.Request.IsHttps;

        httpContext.Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(_jwtOptions.AccessTokenMinutes)
        });

        // Refresh token cookie is scoped to the refresh endpoint only
        httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth/refresh",
            MaxAge = TimeSpan.FromDays(_jwtOptions.RefreshTokenDays)
        });
    }
}

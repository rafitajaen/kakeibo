using Kakeibo.Api.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using System.Security.Claims;
using System.Text;

namespace Kakeibo.Api.Infrastructure.Auth;

// Generates and validates JWT access tokens using NodaTime for all timestamp calculations.
public sealed class JwtService(IOptions<JwtOptions> options, IClock clock)
{
    private readonly JwtOptions _options = options.Value;

    // Generates a signed JWT access token for the given user.
    // Sets exp/iat/nbf as Unix second timestamps to avoid DateTime usage.
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = clock.GetCurrentInstant();
        var expiry = now.Plus(Duration.FromMinutes(_options.AccessTokenMinutes));

        // Use Unix timestamps in claims to avoid DateTime — exp/iat/nbf are defined as numbers in RFC 7519
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
            [JwtRegisteredClaimNames.Email] = user.Email,
            [ClaimTypes.Role] = user.Role.ToString(),
            [JwtRegisteredClaimNames.Iat] = now.ToUnixTimeSeconds(),
            [JwtRegisteredClaimNames.Nbf] = now.ToUnixTimeSeconds(),
            [JwtRegisteredClaimNames.Exp] = expiry.ToUnixTimeSeconds()
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }

    // Validates a JWT token asynchronously and returns the principal if valid, or null if invalid.
    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var handler = new JsonWebTokenHandler();

        var result = await handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        });

        if (!result.IsValid)
            return null;

        return new ClaimsPrincipal(result.ClaimsIdentity);
    }
}

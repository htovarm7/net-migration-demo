using System.Security.Claims;
using System.Text;
using AuthManager.Api.Data;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthManager.Api.Auth;

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opts = options.Value;

    public (string Token, DateTime ExpiresUtc) Create(AppUser user, Guid? appGuid)
    {
        var expires = DateTime.UtcNow.AddMinutes(_opts.ExpiresMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? "")
        };
        if (appGuid is { } g) claims.Add(new Claim("app_guid", g.ToString()));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _opts.Issuer,
            Audience = _opts.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}

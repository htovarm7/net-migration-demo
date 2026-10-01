using AuthManager.Api.Data;
using AuthManager.Api.Routing;
using Microsoft.AspNetCore.Identity;

namespace AuthManager.Api.Auth;

public record LoginRequest(string User, string Password, Guid? Guid = null, string? ReturnPath = null);

public record LoginResult(
    string Token,
    DateTime ExpiresUtc,
    bool HashMigrated,
    string Destination,
    string RedirectUrl);

public class LoginService(
    UserManager<AppUser> users,
    TokenService tokens,
    RouteResolver router,
    ILogger<LoginService> logger)
{
    /// <summary>
    /// Validates credentials. If the hash was in .NET Framework format (v2) and the password is correct,
    /// Identity regenerates it as v3 (UserManager.CheckPasswordAsync rehashes automatically).
    /// Returns null when the credentials are invalid or the account is locked out.
    /// </summary>
    public async Task<LoginResult?> LoginAsync(LoginRequest req)
    {
        var user = await users.FindByNameAsync(req.User);
        if (user is null) return null;

        if (await users.IsLockedOutAsync(user)) return null;

        var formatBefore = PasswordHashFormatDetector.Detect(user.PasswordHash);

        if (!await users.CheckPasswordAsync(user, req.Password))
        {
            await users.AccessFailedAsync(user);
            return null;
        }

        await users.ResetAccessFailedCountAsync(user);

        var formatAfter = PasswordHashFormatDetector.Detect(user.PasswordHash);
        var migrated = formatBefore == PasswordHashFormat.IdentityV2 && formatAfter == PasswordHashFormat.IdentityV3;
        if (migrated) logger.LogInformation("{User}'s hash migrated from Identity v2 to v3", user.UserName);

        var (token, expires) = tokens.Create(user, req.Guid);
        var destination = router.Resolve(req.ReturnPath ?? "/");

        return new LoginResult(token, expires, migrated, destination.Destination.ToString(), destination.Url);
    }
}

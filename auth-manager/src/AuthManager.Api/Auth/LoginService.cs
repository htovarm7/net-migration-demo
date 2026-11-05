using AuthManager.Api.Companies;
using AuthManager.Api.Data;
using AuthManager.Api.Routing;
using AuthManager.Api.Session;
using Microsoft.AspNetCore.Identity;

namespace AuthManager.Api.Auth;

public record LoginRequest(string User, string Password, Guid? Guid = null, string? ReturnPath = null);

public record LoginResult(
    string Token,
    DateTime ExpiresUtc,
    bool HashMigrated,
    string Destination,
    string RedirectUrl,
    string CompanyId,
    string CompanyName);

public class LoginService(
    UserManager<AppUser> users,
    TokenService tokens,
    RouteResolver router,
    SessionStateStore sessionStore,
    ILogger<LoginService> logger)
{
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
        if (migrated)
            logger.LogInformation("{User}: hash migrated (v2 -> v3)", user.UserName);
        else if (formatAfter == PasswordHashFormat.IdentityV3)
            logger.LogInformation("{User}: hash already on v3, no migration needed", user.UserName);

        var (token, expires) = tokens.Create(user, req.Guid);
        var destination = router.Resolve(req.ReturnPath ?? "/");

        var company = CompanyDirectory.Resolve(user.UserName!);
        var profile = UserDirectory.Resolve(user.UserName!);
        var session = new SessionContext(
            user.Id, user.UserName!, company.CompanyId, company.CompanyName,
            profile.FirstName, profile.LastName, profile.Role);
        await sessionStore.SetAsync(session, expires - DateTime.UtcNow);

        return new LoginResult(
            token, expires, migrated, destination.Destination.ToString(), destination.Url,
            company.CompanyId, company.CompanyName);
    }
}

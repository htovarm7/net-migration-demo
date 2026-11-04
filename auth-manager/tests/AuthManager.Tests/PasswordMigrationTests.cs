using AuthManager.Api.Auth;
using AuthManager.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace AuthManager.Tests;

public class PasswordMigrationTests
{
    private readonly PasswordHasher<AppUser> _hasher = new();
    private readonly AppUser _user = new() { UserName = "x" };

    [Theory]
    [InlineData("12345678", SeedData.HashLegacy1)]
    [InlineData("AbcD$1234", SeedData.HashLegacy2)]
    public void LegacyHash_WithCorrectPassword_VerifiesAndRequestsRehash(string password, string hash)
    {
        Assert.Equal(PasswordHashFormat.IdentityV2, PasswordHashFormatDetector.Detect(hash));

        var r = _hasher.VerifyHashedPassword(_user, hash, password);

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, r);
    }

    [Theory]
    [InlineData("incorrecta", SeedData.HashLegacy1)]
    [InlineData("12345678", SeedData.HashLegacy2)]
    public void LegacyHash_WithWrongPassword_Fails(string password, string hash)
    {
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(_user, hash, password));
    }

    [Fact]
    public void NewHash_IsV3_AndVerifiesWithoutRehash()
    {
        var nuevo = _hasher.HashPassword(_user, "12345678");

        Assert.Equal(PasswordHashFormat.IdentityV3, PasswordHashFormatDetector.Detect(nuevo));
        Assert.Equal(PasswordVerificationResult.Success, _hasher.VerifyHashedPassword(_user, nuevo, "12345678"));
    }
}

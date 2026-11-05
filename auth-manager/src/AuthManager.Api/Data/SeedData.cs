namespace AuthManager.Api.Data;

// Passwords: user1 = "12345678", user2 = "AbcD$1234".
public static class SeedData
{
    public const string HashLegacy1 = "AOyYE92I1JyDIIUuYnPQ0m+sZD4jJ24Tnf51CawUSg5c5eOGu2t19utyI2CgqAVlfg==";
    public const string HashLegacy2 = "AOxUJgv030oo4GZJuXJEyAeOK6G8LVxiswQI1qu9y0ORqK+V7XndRVN4PLAlQ/fQPA==";

    public static async Task InitializeAsync(AuthDbContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        Add(db, "user1", HashLegacy1);
        Add(db, "user2", HashLegacy2);
        await db.SaveChangesAsync();
    }

    private static void Add(AuthDbContext db, string userName, string hash)
    {
        db.Users.Add(new AppUser
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            PasswordHash = hash,
            SecurityStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true
        });
    }
}

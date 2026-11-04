namespace AuthManager.Api.Companies;

public record UserProfile(string FirstName, string LastName, string Role);

/// <summary>
/// Mock profile data, same reasoning as CompanyDirectory: the real values would come
/// from MiiIdentidad, which we don't have access to.
/// </summary>
public static class UserDirectory
{
    private static readonly Dictionary<string, UserProfile> ByUserName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user1"] = new UserProfile("Hector", "Tovar", "Administrator"),
        ["user2"] = new UserProfile("Sofia", "Ramirez", "Operator")
    };

    public static UserProfile Resolve(string userName) =>
        ByUserName.TryGetValue(userName, out var profile)
            ? profile
            : new UserProfile(userName, "", "User");
}

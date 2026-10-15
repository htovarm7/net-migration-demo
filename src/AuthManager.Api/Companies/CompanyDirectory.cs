namespace AuthManager.Api.Companies;

public record CompanyInfo(string CompanyId, string CompanyName);

/// <summary>
/// Mock company data. In the real system this comes from a cross-database join
/// between MiiIdentidad (UserMiiCaja) and MiiCaja (Companies); we don't have access
/// to those databases, so this stands in for that lookup.
/// </summary>
public static class CompanyDirectory
{
    private static readonly Dictionary<string, CompanyInfo> ByUserName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user1"] = new CompanyInfo("CMP-001", "Ferreteria El Tornillo"),
        ["user2"] = new CompanyInfo("CMP-002", "Abarrotes Dona Lupe")
    };

    public static CompanyInfo Resolve(string userName) =>
        ByUserName.TryGetValue(userName, out var company)
            ? company
            : new CompanyInfo("CMP-000", "Sin empresa asignada");
}

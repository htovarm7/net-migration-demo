namespace AuthManager.Api.Companies;

public record CompanyInfo(string CompanyId, string CompanyName);

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

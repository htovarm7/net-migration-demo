namespace ModernWeb.Services;

public static class MockSales
{
    public record MonthlySummary(decimal Total, int Orders);

    public static MonthlySummary For(string? companyId)
    {
        var seed = string.IsNullOrEmpty(companyId) ? 0 : companyId.GetHashCode();
        var random = new Random(seed);
        return new MonthlySummary(
            Math.Round((decimal)(random.NextDouble() * 50000), 2),
            random.Next(10, 200));
    }
}

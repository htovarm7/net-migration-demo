using System.Text.Json;
using StackExchange.Redis;

namespace AuthManager.Api.Session;

public class SessionStateStore(IConnectionMultiplexer redis, ILogger<SessionStateStore> logger)
{
    private static string Key(string userId) => $"session:{userId}";

    public async Task SetAsync(SessionContext context, TimeSpan ttl)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(Key(context.UserId), JsonSerializer.Serialize(context), ttl);
        logger.LogInformation(
            "{User}: session saved (company={CompanyId} {CompanyName}, ttl={Ttl}m)",
            context.UserName, context.CompanyId, context.CompanyName, (int)ttl.TotalMinutes);
    }

    public async Task<SessionContext?> GetAsync(string userId)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(Key(userId));
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<SessionContext>((string)value!);
    }
}

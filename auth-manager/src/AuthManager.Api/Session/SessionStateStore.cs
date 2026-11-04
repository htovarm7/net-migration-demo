using System.Text.Json;
using StackExchange.Redis;

namespace AuthManager.Api.Session;

/// <summary>
/// Redis-backed session state, keyed by user id. Both the legacy and the modern app
/// read the same key through Auth Manager's /session endpoint, so they always see the
/// same company context for a user no matter which one asks.
/// </summary>
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

using System.Net.Http.Headers;
using System.Text.Json;

namespace ModernWeb.Services;

public record SessionContext(string UserId, string UserName, string CompanyId, string CompanyName);

/// <summary>
/// Reads session/company state from Auth Manager's /session endpoint, which is itself
/// backed by Redis. The legacy app reads the exact same key, so both apps always agree.
/// </summary>
public class SessionClient(HttpClient http)
{
    public async Task<SessionContext?> GetAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SessionContext>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}

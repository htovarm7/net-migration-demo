using System.Net.Http.Headers;
using System.Text.Json;

namespace ModernWeb.Services;

public record SessionContext(
    string UserId,
    string UserName,
    string CompanyId,
    string CompanyName,
    string FirstName,
    string LastName,
    string Role);

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

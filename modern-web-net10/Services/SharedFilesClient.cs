using System.Net.Http.Headers;
using System.Text.Json;

namespace ModernWeb.Services;

public record SharedUploadRecord(string FileName, string UploadedBy, string SourceApp, DateTime UploadedAtUtc, long SizeBytes);

/// <summary>
/// Calls Auth Manager's global /uploads endpoint - the same contract the legacy app's
/// AuthManagerClient.UploadShared/ListShared use. Neither app owns this data.
/// </summary>
public class SharedFilesClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<SharedUploadRecord>> ListAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/uploads");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return [];

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<SharedUploadRecord>>(json, JsonOptions) ?? [];
    }

    public async Task UploadAsync(string token, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent("modern"), "source");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/uploads") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await http.SendAsync(request);
    }
}

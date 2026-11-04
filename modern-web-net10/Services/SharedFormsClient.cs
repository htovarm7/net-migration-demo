using System.Net.Http.Headers;
using System.Text.Json;

namespace ModernWeb.Services;

public record FormSubmissionRecord(
    string Id, string FirstName, string LastName, string Email,
    string SubmittedBy, string SourceApp, DateTime SubmittedAtUtc,
    string? FileName, long? FileSizeBytes);

/// <summary>
/// Calls Auth Manager's global /forms endpoint - the same contract and same history
/// the legacy app's AuthManagerClient.SubmitSharedForm/ListSharedForms use.
/// </summary>
public class SharedFormsClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<FormSubmissionRecord>> ListAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/forms");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return [];

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<FormSubmissionRecord>>(json, JsonOptions) ?? [];
    }

    public async Task SubmitAsync(string token, string firstName, string lastName, string email, IFormFile? file)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(firstName ?? ""), "firstName" },
            { new StringContent(lastName ?? ""), "lastName" },
            { new StringContent(email ?? ""), "email" },
            { new StringContent("modern"), "source" }
        };

        if (file is { Length: > 0 })
        {
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, "file", file.FileName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "/forms") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await http.SendAsync(request);
    }
}

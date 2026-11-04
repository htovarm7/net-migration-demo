using System.Text.Json;

namespace AuthManager.Api.Forms;

public record FormSubmission(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string SubmittedBy,
    string SourceApp,
    DateTime SubmittedAtUtc,
    string? FileName,
    long? FileSizeBytes);

/// <summary>
/// A global endpoint for structured data, not just files: same /forms contract for
/// legacy and modern, same stored history either one can read back - "informacion del
/// pasado" visible from both, and from the terminal via GET /forms.
/// </summary>
public class SharedFormStore(IWebHostEnvironment env)
{
    private string RootPath => Path.Combine(env.ContentRootPath, "App_Data", "SharedForms");

    public async Task<FormSubmission> SaveAsync(
        string firstName, string lastName, string email, string submittedBy, string sourceApp, IFormFile? file)
    {
        Directory.CreateDirectory(RootPath);
        var id = Guid.NewGuid().ToString("N")[..8];

        string? fileName = null;
        long? fileSize = null;
        if (file is { Length: > 0 })
        {
            fileName = Path.GetFileName(file.FileName);
            fileSize = file.Length;
            await using var fileStream = File.Create(Path.Combine(RootPath, $"{id}.bin"));
            await file.CopyToAsync(fileStream);
        }

        var submission = new FormSubmission(
            id, firstName, lastName, email, submittedBy, sourceApp, DateTime.UtcNow, fileName, fileSize);

        await File.WriteAllTextAsync(Path.Combine(RootPath, $"{id}.json"), JsonSerializer.Serialize(submission));
        return submission;
    }

    public List<FormSubmission> List()
    {
        if (!Directory.Exists(RootPath)) return [];

        return new DirectoryInfo(RootPath).GetFiles("*.json")
            .Select(f => JsonSerializer.Deserialize<FormSubmission>(File.ReadAllText(f.FullName)))
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .ToList();
    }
}

namespace AuthManager.Api.Uploads;

public record SharedUploadRecord(
    string FileName,
    string UploadedBy,
    string SourceApp,
    DateTime UploadedAtUtc,
    long SizeBytes);

/// <summary>
/// A genuinely global endpoint: neither legacy nor modern owns this data. Both apps call
/// the exact same contract (POST/GET /uploads) and see the exact same files, regardless
/// of which one the request came from. Metadata is encoded in the stored file name so no
/// separate index needs to be kept in sync.
/// </summary>
public class SharedUploadStore(IWebHostEnvironment env)
{
    private const string Separator = "__";
    private string RootPath => Path.Combine(env.ContentRootPath, "App_Data", "SharedUploads");

    public async Task<SharedUploadRecord> SaveAsync(IFormFile file, string uploadedBy, string sourceApp)
    {
        Directory.CreateDirectory(RootPath);

        var id = Guid.NewGuid().ToString("N")[..8];
        var safeUploadedBy = Sanitize(uploadedBy);
        var safeSourceApp = Sanitize(sourceApp);
        var safeOriginalName = Path.GetFileName(file.FileName);
        var storedName = $"{id}{Separator}{safeSourceApp}{Separator}{safeUploadedBy}{Separator}{safeOriginalName}";

        await using var stream = File.Create(Path.Combine(RootPath, storedName));
        await file.CopyToAsync(stream);

        return Describe(new FileInfo(Path.Combine(RootPath, storedName)));
    }

    public List<SharedUploadRecord> List()
    {
        if (!Directory.Exists(RootPath)) return [];

        return new DirectoryInfo(RootPath).GetFiles()
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(Describe)
            .ToList();
    }

    private static SharedUploadRecord Describe(FileInfo file)
    {
        var parts = file.Name.Split(Separator, 4);
        var sourceApp = parts.Length > 1 ? parts[1] : "unknown";
        var uploadedBy = parts.Length > 2 ? parts[2] : "unknown";
        var originalName = parts.Length > 3 ? parts[3] : file.Name;

        return new SharedUploadRecord(originalName, uploadedBy, sourceApp, file.CreationTimeUtc, file.Length);
    }

    private static string Sanitize(string value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value.Replace(Separator, "_").Replace("/", "_").Replace("\\", "_");
}

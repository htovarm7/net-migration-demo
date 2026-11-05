using Microsoft.AspNetCore.Mvc;
using ModernWeb.Services;

namespace ModernWeb.Controllers;

public class UploadsController(IWebHostEnvironment env) : Controller
{
    private string UploadsPath => Path.Combine(env.ContentRootPath, "UploadedFiles");

    public IActionResult Index()
    {
        Directory.CreateDirectory(UploadsPath);
        ViewBag.Company = HttpContext.Items["Company"] as SessionContext;
        ViewBag.Files = new DirectoryInfo(UploadsPath).GetFiles()
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => f.Name).ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(IFormFile file)
    {
        if (file is { Length: > 0 })
        {
            Directory.CreateDirectory(UploadsPath);
            var safeName = Guid.NewGuid().ToString("N")[..8] + "_" + Path.GetFileName(file.FileName);
            await using var stream = System.IO.File.Create(Path.Combine(UploadsPath, safeName));
            await file.CopyToAsync(stream);
        }

        return RedirectToAction("Index");
    }
}

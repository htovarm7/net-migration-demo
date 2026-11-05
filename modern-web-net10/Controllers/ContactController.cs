using Microsoft.AspNetCore.Mvc;
using ModernWeb.Models;

namespace ModernWeb.Controllers;

public class ContactController(IWebHostEnvironment env) : Controller
{
    public IActionResult Index() => View(new ContactForm());

    [HttpPost]
    public async Task<IActionResult> Index([FromForm] ContactForm model, IFormFile? file)
    {
        ViewBag.RawForm = Request.Form;
        ViewBag.BoundModel = model;

        if (file is { Length: > 0 })
        {
            ViewBag.FileName = file.FileName;
            ViewBag.FileContentType = file.ContentType;
            ViewBag.FileLength = file.Length;

            var dir = Path.Combine(env.ContentRootPath, "ContactUploads");
            Directory.CreateDirectory(dir);
            var safeName = Guid.NewGuid().ToString("N")[..8] + "_" + Path.GetFileName(file.FileName);
            await using var stream = System.IO.File.Create(Path.Combine(dir, safeName));
            await file.CopyToAsync(stream);
            ViewBag.SavedAs = safeName;
        }

        return View("Result");
    }
}

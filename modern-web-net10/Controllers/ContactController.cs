using Microsoft.AspNetCore.Mvc;
using ModernWeb.Models;

namespace ModernWeb.Controllers;

// Not a migrated page: lives entirely in the modern app. The form posts to this
// same controller/host, so a GET here is always answered by this app, never legacy.
public class ContactController(IWebHostEnvironment env) : Controller
{
    public IActionResult Index() => View(new ContactForm());

    [HttpPost]
    public async Task<IActionResult> Index([FromForm] ContactForm model, IFormFile? file)
    {
        // 1) The raw dictionary exactly as it arrived on the wire.
        ViewBag.RawForm = Request.Form;

        // 2) The same data, already bound into a typed object by model binding.
        ViewBag.BoundModel = model;

        // 3) The multipart file part, inspected directly in the controller - not just
        // saved blind. IFormFile exposes name/type/size without reading bytes.
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

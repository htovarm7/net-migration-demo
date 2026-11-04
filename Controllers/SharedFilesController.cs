using Microsoft.AspNetCore.Mvc;
using ModernWeb.Services;

namespace ModernWeb.Controllers;

// The opposite of Uploads: this page talks to Auth Manager's global /uploads endpoint,
// so whatever is submitted here (or from the legacy app's own SharedFiles page) shows
// up in both - on purpose.
public class SharedFilesController(SharedFilesClient client) : Controller
{
    public async Task<IActionResult> Index()
    {
        var token = Request.Cookies["am_token"];
        ViewBag.Files = token != null ? await client.ListAsync(token) : new List<SharedUploadRecord>();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(IFormFile? file)
    {
        var token = Request.Cookies["am_token"];
        if (token != null && file is { Length: > 0 })
            await client.UploadAsync(token, file);

        return RedirectToAction("Index");
    }
}

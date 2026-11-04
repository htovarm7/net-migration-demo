using Microsoft.AspNetCore.Mvc;
using ModernWeb.Services;

namespace ModernWeb.Controllers;

// Like SharedFiles, but for a real form: named fields plus an optional file, posted to
// Auth Manager's global /forms endpoint - the legacy app's own Shared Forms page reads
// back the exact same history.
public class SharedFormsController(SharedFormsClient client) : Controller
{
    public async Task<IActionResult> Index()
    {
        var token = Request.Cookies["am_token"];
        ViewBag.Submissions = token != null ? await client.ListAsync(token) : new List<FormSubmissionRecord>();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string firstName, string lastName, string email, IFormFile? file)
    {
        var token = Request.Cookies["am_token"];
        if (token != null)
            await client.SubmitAsync(token, firstName, lastName, email, file);

        return RedirectToAction("Index");
    }
}

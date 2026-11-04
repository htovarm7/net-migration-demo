using Microsoft.AspNetCore.Mvc;
using ModernWeb.Services;

namespace ModernWeb.Controllers;

public class ProfileController : Controller
{
    public IActionResult Index()
    {
        ViewBag.Company = HttpContext.Items["Company"] as SessionContext;
        return View();
    }
}

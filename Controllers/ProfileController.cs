using Microsoft.AspNetCore.Mvc;

namespace ModernWeb.Controllers;

public class ProfileController : Controller
{
    public IActionResult Index() => View();
}

using Microsoft.AspNetCore.Mvc;

namespace ModernWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}

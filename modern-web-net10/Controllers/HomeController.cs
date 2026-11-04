using Microsoft.AspNetCore.Mvc;
using ModernWeb.Services;

namespace ModernWeb.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var company = HttpContext.Items["Company"] as SessionContext;
        ViewBag.Company = company;
        ViewBag.Sales = MockSales.For(company?.CompanyId);
        return View();
    }
}

using System.Web.Mvc;
using LegacyWeb.Services;

namespace LegacyWeb.Controllers
{
    public class OrdersController : Controller
    {
        public ActionResult Index()
        {
            var session = HttpContext.Items["Session"] as SessionResult;
            ViewBag.Session = session;
            ViewBag.Orders = MockOrders.For(session?.CompanyId);
            return View();
        }
    }
}

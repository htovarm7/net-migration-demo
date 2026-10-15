using System.Collections.Generic;
using System.Web.Mvc;

namespace LegacyWeb.Controllers
{
    public class OrdersController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Orders = new List<string> { "Order #1001 - Pending", "Order #1002 - Shipped", "Order #1003 - Delivered" };
            return View();
        }
    }
}

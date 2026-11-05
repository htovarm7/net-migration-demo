using System.Web.Mvc;

namespace LegacyWeb.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return Content(string.Empty);
        }
    }
}

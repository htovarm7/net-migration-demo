using System.Web.Mvc;

namespace LegacyWeb.Controllers
{
    public class ProfileController : Controller
    {
        public ActionResult Index()
        {
            return Content(string.Empty);
        }
    }
}

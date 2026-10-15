using System.Web.Mvc;

namespace LegacyWeb.Controllers
{
    // Migrated page: kept here only so the routing filter can redirect it to the new app.
    public class ProfileController : Controller
    {
        public ActionResult Index()
        {
            return Content(string.Empty);
        }
    }
}

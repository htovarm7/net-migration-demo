using System.Web;
using System.Web.Mvc;
using LegacyWeb.Filters;
using LegacyWeb.Services;

namespace LegacyWeb.Controllers
{
    // The opposite of Uploads: this page talks to Auth Manager's global /uploads
    // endpoint, so whatever is submitted here (or from the modern app's own
    // SharedFiles page) shows up in both - on purpose.
    public class SharedFilesController : Controller
    {
        public ActionResult Index()
        {
            var token = Request.Cookies[AuthRoutingFilter.TokenCookie]?.Value;
            ViewBag.Files = token != null ? new AuthManagerClient().ListShared(token) : null;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(HttpPostedFileBase file)
        {
            var token = Request.Cookies[AuthRoutingFilter.TokenCookie]?.Value;
            if (token != null && file != null && file.ContentLength > 0)
                new AuthManagerClient().UploadShared(token, file);

            return RedirectToAction("Index");
        }
    }
}

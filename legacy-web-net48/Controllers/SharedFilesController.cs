using System.Web;
using System.Web.Mvc;
using LegacyWeb.Filters;
using LegacyWeb.Services;

namespace LegacyWeb.Controllers
{
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

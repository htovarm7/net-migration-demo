using System.Web;
using System.Web.Mvc;
using LegacyWeb.Filters;
using LegacyWeb.Services;

namespace LegacyWeb.Controllers
{
    // Like SharedFiles, but for a real form: named fields plus an optional file, posted
    // to Auth Manager's global /forms endpoint - the modern app's own Shared Forms page
    // reads back the exact same history.
    public class SharedFormsController : Controller
    {
        public ActionResult Index()
        {
            var token = Request.Cookies[AuthRoutingFilter.TokenCookie]?.Value;
            ViewBag.Submissions = token != null ? new AuthManagerClient().ListSharedForms(token) : null;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string firstName, string lastName, string email, HttpPostedFileBase file)
        {
            var token = Request.Cookies[AuthRoutingFilter.TokenCookie]?.Value;
            if (token != null)
                new AuthManagerClient().SubmitSharedForm(token, firstName, lastName, email, file);

            return RedirectToAction("Index");
        }
    }
}

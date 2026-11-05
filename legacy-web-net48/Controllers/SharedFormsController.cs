using System.Web;
using System.Web.Mvc;
using LegacyWeb.Filters;
using LegacyWeb.Services;

namespace LegacyWeb.Controllers
{
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

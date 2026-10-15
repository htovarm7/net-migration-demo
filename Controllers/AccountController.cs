using System.Web;
using System.Web.Mvc;
using LegacyWeb.Filters;
using LegacyWeb.Models;
using LegacyWeb.Services;

namespace LegacyWeb.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Login(string returnPath = "/Orders")
        {
            return View(new LoginViewModel { ReturnPath = returnPath });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = new AuthManagerClient().Login(model.User, model.Password, model.ReturnPath);
            if (result == null)
            {
                ModelState.AddModelError("", "Invalid user or password.");
                return View(model);
            }

            Response.Cookies.Add(new HttpCookie(AuthRoutingFilter.TokenCookie, result.Token)
            {
                HttpOnly = true,
                Expires = result.ExpiresUtc
            });

            return Redirect(result.RedirectUrl);
        }

        public ActionResult Logout()
        {
            Response.Cookies.Add(new HttpCookie(AuthRoutingFilter.TokenCookie)
            {
                Expires = System.DateTime.UtcNow.AddDays(-1)
            });
            return RedirectToAction("Login");
        }
    }
}

using System.Web;
using System.Web.Mvc;
using LegacyWeb.Services;

namespace LegacyWeb.Filters
{
    public class AuthRoutingFilter : IActionFilter
    {
        public const string TokenCookie = "am_token";

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            if (controller == "Account") return;

            var path = filterContext.HttpContext.Request.Url.AbsolutePath;
            var cookie = filterContext.HttpContext.Request.Cookies[TokenCookie];

            if (cookie == null || string.IsNullOrEmpty(cookie.Value))
            {
                filterContext.Result = LoginRedirect(filterContext, path);
                return;
            }

            var route = new AuthManagerClient().Route(path, cookie.Value);
            if (route == null)
            {
                filterContext.HttpContext.Response.Cookies.Add(new HttpCookie(TokenCookie) { Expires = System.DateTime.UtcNow.AddDays(-1) });
                filterContext.Result = LoginRedirect(filterContext, path);
                return;
            }

            if (route.Destination == "Modern")
                filterContext.Result = new RedirectResult(route.Url);
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        private static ActionResult LoginRedirect(ActionExecutingContext filterContext, string returnPath)
        {
            var url = new UrlHelper(filterContext.RequestContext)
                .Action("Login", "Account", new { returnPath });
            return new RedirectResult(url);
        }
    }
}

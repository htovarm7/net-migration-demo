using System.Web;
using System.Web.Mvc;
using LegacyWeb.Services;

namespace LegacyWeb.Filters
{
    public class AuthRoutingFilter : IActionFilter
    {
        public const string TokenCookie = "am_token";
        public const string CookieDomain = "miicel.local";

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
                filterContext.HttpContext.Response.Cookies.Add(new HttpCookie(TokenCookie) { Domain = CookieDomain, Expires = System.DateTime.UtcNow.AddDays(-1) });
                filterContext.Result = LoginRedirect(filterContext, path);
                return;
            }

            if (route.Destination == "Modern")
            {
                filterContext.Result = CrossAppRedirect(filterContext, route.Url);
                return;
            }

            filterContext.HttpContext.Items["Session"] = new AuthManagerClient().Session(cookie.Value);
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

        // A 302 on a POST/PUT gets re-sent as a GET with no body by every browser - any form
        // data or uploaded file would silently vanish. 307 is the one status code the HTTP
        // spec guarantees preserves both the method and the body across the redirect.
        private static ActionResult CrossAppRedirect(ActionExecutingContext filterContext, string url)
        {
            var method = filterContext.HttpContext.Request.HttpMethod;
            if (method == "GET" || method == "HEAD")
                return new RedirectResult(url);

            return new TemporaryRedirect307Result(url);
        }

        private class TemporaryRedirect307Result : ActionResult
        {
            private readonly string _url;
            public TemporaryRedirect307Result(string url) { _url = url; }

            public override void ExecuteResult(ControllerContext context)
            {
                context.HttpContext.Response.StatusCode = 307;
                context.HttpContext.Response.AddHeader("Location", _url);
            }
        }
    }
}

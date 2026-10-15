using System.Web.Mvc;
using System.Web.Routing;
using LegacyWeb.App_Start;
using LegacyWeb.Filters;

namespace LegacyWeb
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalFilters.Filters.Add(new AuthRoutingFilter());
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}

using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Collections;

namespace hidMy
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Session_Start()
        {
            Session["itemSelecionado"] = "";
            Session["ContasCollapsedList"] = new ArrayList();
            Session["DefaultItemsPerPage"] = "7";
        }
    }
}

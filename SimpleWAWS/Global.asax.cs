using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Routing;
using SimpleWAWS.Authentication;
using SimpleWAWS.Code;

namespace SimpleWAWS
{
    public class SimpleWawsService : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //Configure Json formatter
            GlobalConfiguration.Configuration.Formatters.Clear();
            GlobalConfiguration.Configuration.Formatters.Add(new JsonMediaTypeFormatter());

            //Templates Routes
            RouteTable.Routes.MapHttpRoute("templates", "api/templates", new { controller = "Templates", action = "Get"});

            //Site Api Routes
            RouteTable.Routes.MapHttpRoute("get-site", "api/site", new { controller = "Site", action = "GetSite" }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("create-site", "api/site", new { controller = "Site", action = "CreateSite" }, new { verb = new HttpMethodConstraint("POST") });
            RouteTable.Routes.MapHttpRoute("get-site-publishing-profile", "api/site/getpublishingprofile", new { controller = "Site", action = "GetPublishingProfile" }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("delete-site", "api/site", new { controller = "Site", action = "DeleteSite" }, new { verb = new HttpMethodConstraint("DELETE") });
            //TODO: this is only for testing. Make sure to remove it later
            RouteTable.Routes.MapHttpRoute("reset-all-free-sites", "api/reset", new { controller = "Site", action = "Reset" }, new { verb = new HttpMethodConstraint("GET") });

            //Register auth provider
            SecurityManager.SetAuthProvider(new AADProvider());
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            if (Request.Path.Equals(ConfigurationManager.AppSettings["RedirectUrl"],
                StringComparison.InvariantCultureIgnoreCase))
            {
                SecurityManager.HandleCallBack(Context);
            }
            else
            {
                SecurityManager.AuthenticateRequest(Context);
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            if (Server != null)
            {
                Exception ex = Server.GetLastError();

                if (Response.StatusCode >= 500)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }
    }
}

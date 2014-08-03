using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Routing;
using Microsoft.ApplicationInsights.Telemetry.Services;
using SimpleWAWS.Authentication;
using SimpleWAWS.Code;

namespace SimpleWAWS
{
    public class SimpleWawsService : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Trace.TraceInformation(">>>>>>>Application started");
            //Configure Json formatter
            GlobalConfiguration.Configuration.Formatters.Clear();
            GlobalConfiguration.Configuration.Formatters.Add(new JsonMediaTypeFormatter());
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.Error = (sender, args) =>
            {
                Trace.TraceError(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            };
            //Templates Routes
            RouteTable.Routes.MapHttpRoute("templates", "api/templates", new { controller = "Templates", action = "Get"});

            //Site Api Routes
            RouteTable.Routes.MapHttpRoute("get-site", "api/site", new { controller = "Site", action = "GetSite" }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("create-site", "api/site", new { controller = "Site", action = "CreateSite" }, new { verb = new HttpMethodConstraint("POST") });
            RouteTable.Routes.MapHttpRoute("get-site-publishing-profile", "api/site/getpublishingprofile", new { controller = "Site", action = "GetPublishingProfile" }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("delete-site", "api/site", new { controller = "Site", action = "DeleteSite" }, new { verb = new HttpMethodConstraint("DELETE") });

            //Admin Only Routes
            RouteTable.Routes.MapHttpRoute("get-all-sites", "api/site/all", new { controller = "Site", action = "All" }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("reset-all-free-sites", "api/site/reset", new { controller = "Site", action = "Reset" }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("reload-all-free-sites", "api/site/reload", new { controller = "Site", action = "DropAndReloadFromAzure" }, new { verb = new HttpMethodConstraint("GET") });

            //Register auth provider
            SecurityManager.SetAuthProvider(new AADProvider());
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            if (!Request.Path.Equals(ConfigurationManager.AppSettings["LoginErrorPage"],
                StringComparison.InvariantCultureIgnoreCase))
            {
                SecurityManager.AuthenticateRequest(Context);
                ServerAnalytics.CurrentRequest.AppUserId = Context.User.Identity.Name;
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

using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Routing;
using SimpleWAWS.Authentication;
using SimpleWAWS.Code;
using System.Web;

namespace SimpleWAWS
{
    public class SimpleWawsService : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Trace.TraceInformation("{0} Application started", AnalyticsEvents.ApplicationStarted);
            //Configure Json formatter
            GlobalConfiguration.Configuration.Formatters.Clear();
            GlobalConfiguration.Configuration.Formatters.Add(new JsonMediaTypeFormatter());
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.Error = (sender, args) =>
            {
                Trace.TraceError(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            };
            //Templates Routes
            RouteTable.Routes.MapHttpRoute("templates", "api/templates", new { controller = "Templates", action = "Get", authenticated = false });

            //Telemetry Routes
            RouteTable.Routes.MapHttpRoute("post-telemetry-event", "api/telemetry/{telemetryEvent}", new { controller = "Telemetry", action = "LogEvent", authenticated = false}, new { verb = new HttpMethodConstraint("POST") });

            //Site Api Routes
            RouteTable.Routes.MapHttpRoute("get-site", "api/site", new { controller = "Site", action = "GetSite", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("create-site", "api/site", new { controller = "Site", action = "CreateSite", authenticated = true }, new { verb = new HttpMethodConstraint("POST") });
            RouteTable.Routes.MapHttpRoute("get-site-publishing-profile", "api/site/getpublishingprofile", new { controller = "Site", action = "GetPublishingProfile", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("get-mobile-client-app", "api/site/mobileclient/{platformString}", new { controller = "Site", action = "GetMobileClientZip", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("delete-site", "api/site", new { controller = "Site", action = "DeleteSite", authenticated = true }, new { verb = new HttpMethodConstraint("DELETE") });

            //Admin Only Routes
            RouteTable.Routes.MapHttpRoute("get-all-sites", "api/site/all", new { controller = "Site", action = "All", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("reset-all-free-sites", "api/site/reset", new { controller = "Site", action = "Reset", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("reload-all-free-sites", "api/site/reload", new { controller = "Site", action = "DropAndReloadFromAzure", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });

            //Register auth provider
            SecurityManager.InitAuthProviders();
        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            if (!SecurityManager.TryAuthenticateSessionCookie(Context))
            {
                if (SecurityManager.HasToken(HttpContext.Current))
                {
                    // This is a login redirect
                    SecurityManager.AuthenticateRequest(Context);
                    return;
                }

                var route = RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current));
                // If the route is not registerd in the WebAPI RouteTable
                //      then it's not an API route, which means it's a resource (*.js, *.css, *.cshtml), not authenticated.
                // If the route doesn't have authenticated value assume true
                var isAuthenticated = route != null && (route.Values["authenticated"] == null || (bool)route.Values["authenticated"]);

                if (isAuthenticated)
                {
                    SecurityManager.AuthenticateRequest(Context);
                }
                else
                {
                    SecurityManager.HandleAnonymousUser(Context);
                }
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

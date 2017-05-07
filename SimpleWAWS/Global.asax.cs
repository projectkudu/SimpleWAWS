using System;
using System.IO;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Routing;
using SimpleWAWS.Authentication;
using System.Web;
using SimpleWAWS.Trace;
using SimpleWAWS.Code;
using Serilog;
using Destructurama;
using Serilog.Filters;
using Serilog.Sinks.Email;
using System.Net;
using System.Linq;
using System.Web.Http.ExceptionHandling;
using Microsoft.ApplicationInsights.Extensibility;

namespace SimpleWAWS
{
    public class SimpleWawsService : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            //Init logger
            InitAppInsights();
            System.Net.ServicePointManager.DefaultConnectionLimit = 12 * SimpleSettings.NUMBER_OF_PROCESSORS;
            var config = GlobalConfiguration.Configuration;
            config.Services.Add(typeof(IExceptionLogger), new TelemetryExceptionLogger());
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

            //Analytics logger
            if (new[]
                {
                    SimpleSettings.EmailPassword,
                    SimpleSettings.EmailServer,
                    SimpleSettings.EmailUserName,
                    SimpleSettings.FromEmail,
                    SimpleSettings.ToEmails
                }.All(s => !string.IsNullOrEmpty(s)))
            {

                var analyticsLogger = new LoggerConfiguration()
                    .Enrich.With(new ExperimentEnricher())
                    .Enrich.With(new UserNameEnricher())
                    .Destructure.JsonNetTypes()
                    .WriteTo.ApplicationInsightsEvents(AppInsights.TelemetryClient)
                    .CreateLogger();

                SimpleTrace.Analytics = analyticsLogger;
                //Diagnostics Logger
                var diagnosticsLogger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .Enrich.With(new ExperimentEnricher())
                    .Enrich.With(new UserNameEnricher())
                    .WriteTo.ApplicationInsightsTraces(AppInsights.TelemetryClient)
                    .WriteTo.Logger(lc => lc
                        .Filter.ByIncludingOnly(Matching.WithProperty<int>("Count", p => p % 10 == 0))
                        .WriteTo.Email(new EmailConnectionInfo
                        {
                            EmailSubject = "TryAppService Alert",
                            EnableSsl = true,
                            FromEmail = SimpleSettings.FromEmail,
                            MailServer = SimpleSettings.EmailServer,
                            NetworkCredentials = new NetworkCredential(SimpleSettings.EmailUserName, SimpleSettings.EmailPassword),
                            Port = 587,
                            ToEmail = SimpleSettings.ToEmails
                        }, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Fatal))
                    .CreateLogger();

                SimpleTrace.Diagnostics = diagnosticsLogger;
            }
            else
            {
                var logger = new LoggerConfiguration().CreateLogger();
                SimpleTrace.Diagnostics = logger;
                SimpleTrace.Analytics = logger;
            }

            SimpleTrace.Diagnostics.Information("Application started");
            //Configure Json formatter
            GlobalConfiguration.Configuration.Formatters.Clear();
            GlobalConfiguration.Configuration.Formatters.Add(new JsonMediaTypeFormatter());
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.Error = (sender, args) =>
            {
                SimpleTrace.Diagnostics.Error(args.ErrorContext.Error.Message);
                args.ErrorContext.Handled = true;
            };

            //Templates Routes
            RouteTable.Routes.MapHttpRoute("templates", "api/templates", new { controller = "Templates", action = "Get", authenticated = false }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("arm-template", "api/armtemplate/{templateName}", new { controller = "Templates", action = "GetARMTemplate", authenticated = false }, new { verb = new HttpMethodConstraint("GET") });

            //Telemetry Routes
            RouteTable.Routes.MapHttpRoute("post-telemetry-event", "api/telemetry/{telemetryEvent}", new { controller = "Telemetry", action = "LogEvent", authenticated = false}, new { verb = new HttpMethodConstraint("POST") });
            RouteTable.Routes.MapHttpRoute("post-feedback-comment", "api/feedback", new { controller = "Telemetry", action = "LogFeedback", authenticated = false }, new { verb = new HttpMethodConstraint("POST") });

            //Resources Api Routes
            RouteTable.Routes.MapHttpRoute("get-resource", "api/resource", new { controller = "Resource", action = "GetResource", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("get-username", "api/resource/user", new { controller = "Resource", action = "GetUserIdentityName", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("create-resource", "api/resource", new { controller = "Resource", action = "CreateResource", authenticated = true }, new { verb = new HttpMethodConstraint("POST") });
            RouteTable.Routes.MapHttpRoute("get-webapp-publishing-profile", "api/resource/getpublishingprofile", new { controller = "Resource", action = "GetWebAppPublishingProfile", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("get-mobile-client-app", "api/resource/mobileclient/{platformString}", new { controller = "Resource", action = "GetMobileClientZip", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("delete-resource", "api/resource", new { controller = "Resource", action = "DeleteResource", authenticated = true }, new { verb = new HttpMethodConstraint("DELETE") });
            RouteTable.Routes.MapHttpRoute("get-resource-status", "api/resource/status", new { controller = "Resource", action = "GetResourceStatus", authenticated = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("extend-resource-expiration-time", "api/resource/extend", new { controller = "Resource", action = "ExtendResourceExpirationTime", authenticated = true }, new { verb = new HttpMethodConstraint("POST") });

            //Admin Only Routes
            RouteTable.Routes.MapHttpRoute("get-all-resources", "api/resource/all/{showFreeResources}", new { controller = "Resource", action = "All", authenticated = true, adminOnly = true , showFreeResources = RouteParameter.Optional}, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("reset-all-free-resources", "api/resource/reset", new { controller = "Resource", action = "Reset", authenticated = true, adminOnly = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("reload-all-free-resources", "api/resource/reload", new { controller = "Resource", action = "DropAndReloadFromAzure", authenticated = true, adminOnly = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("delete-users-resource", "api/resource/delete/{userIdentity}", new { controller = "Resource", action = "DeleteUserResource", authenticated = true, adminOnly = true }, new { verb = new HttpMethodConstraint("GET") });
            RouteTable.Routes.MapHttpRoute("cleanup-subscriptions", "api/resource/runcleanup", new { controller = "Resource", action = "RunCleanupSubscriptions", authenticated = true, adminOnly = true }, new { verb = new HttpMethodConstraint("GET") });
            
            //Register auth provider
            SecurityManager.InitAuthProviders();
            ResourcesManager.GetInstanceAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private void InitAppInsights()
        {
            var key = SimpleSettings.AppInsightsInstrumentationKey;
            if (!string.IsNullOrEmpty(key))
            {
                TelemetryConfiguration.Active.InstrumentationKey = key;
                TelemetryConfiguration.Active.DisableTelemetry = false;
            }
            else
            {
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }
        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {
            var context = new HttpContextWrapper(HttpContext.Current);
            ExperimentManager.AssignExperiment(context);
            GlobalizationManager.SetCurrentCulture(context);

            context.Response.Headers["Access-Control-Expose-Headers"] = "LoginUrl";

        }
        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            var context = new HttpContextWrapper(HttpContext.Current);

            if (!string.IsNullOrEmpty(AuthSettings.EnableAuth) &&
                AuthSettings.EnableAuth.Equals(false.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                context.User = new TryWebsitesPrincipal(new TryWebsitesIdentity("user@localhost.com", null, "Local"));
                return;
            }

            if (!SecurityManager.TryAuthenticateSessionCookie(context))
            {
                // Support requests from non-browsers with bearer headers
                if (context.IsFunctionsPortalBackendRequest() && !context.IsBrowserRequest() &&
                    SecurityManager.TryAuthenticateBearer(context))
                {
                    return;
                }

                if (SecurityManager.HasToken(context))
                {
                    // This is a login 
                    SecurityManager.AuthenticateRequest(context);
                    return;
                }

                var route = RouteTable.Routes.GetRouteData(context);
                // If the route is not registered in the WebAPI RouteTable
                //    then it's not an API route, which means it's a resource (*.js, *.css, *.cshtml), not authenticated.
                // If the route doesn't have authenticated value assume true
                var isAuthenticated = route != null && (route.Values["authenticated"] == null || (bool) route.Values["authenticated"]);

                if (isAuthenticated)
                {
                    SecurityManager.AuthenticateRequest(context);
                }
                else if (context.IsBrowserRequest())
                {
                    SecurityManager.HandleAnonymousUser(context);
                }
            }
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            if (Server != null)
            {
                Exception ex = Server.GetLastError();

                if (Response.StatusCode > 403)
                {
                    AppInsights.TelemetryClient.TrackException(ex);
                }
            }
        }
        //https://github.com/serilog/serilog-sinks-applicationinsights
        protected void Application_Shutdown(object sender, EventArgs e)
        {

            AppInsights.TelemetryClient.Flush();

            // The AI Documentation mentions that calling .Flush() *can* be asynchronous and non-blocking so
            // depending on the underlying Channel to AI you might want to wait some time
            // specific to your application and its connectivity constraints for the flush to finish.

            System.Threading.Thread.Sleep(1000);

        }
    }
}

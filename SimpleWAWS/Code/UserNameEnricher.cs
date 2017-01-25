using Serilog.Core;
using Serilog.Events;
using System.Web;

namespace SimpleWAWS.Code
{
    public class UserNameEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var userName = HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null
                ? "NoUserContext"
                : HttpContext.Current.User.Identity.Name;

            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("UserName", userName));
        }
    }
}
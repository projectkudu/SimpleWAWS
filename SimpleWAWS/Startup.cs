using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SimpleWAWS.Startup))]
namespace SimpleWAWS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

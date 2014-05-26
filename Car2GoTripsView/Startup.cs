using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Car2GoTripsView.Startup))]
namespace Car2GoTripsView
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}

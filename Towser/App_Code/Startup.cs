using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Towser.Startup))]

namespace Towser
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // hub route
            app.MapSignalR();

            // persistent connection route
            app.MapSignalR<Towser.Pcon.TowserPcon>("/towserPcon");
        }
    }
}

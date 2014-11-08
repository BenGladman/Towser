using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Telser.Startup))]

namespace Telser
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // hub route
            app.MapSignalR();

            // persistent connection route
            app.MapSignalR<TelserPersistentConnection>("/telser");
        }
    }
}

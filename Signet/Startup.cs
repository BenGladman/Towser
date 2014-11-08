using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Signet.Startup))]

namespace Signet
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // hub route
            app.MapSignalR();

            // persistent connection route
            app.MapSignalR<TelnetConnection>("/telnet");
        }
    }
}

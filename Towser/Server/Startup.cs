﻿using System;
using System.Threading.Tasks;
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
            app.MapSignalR<Perseus.PerseusConnection>("/perseus");
        }
    }
}

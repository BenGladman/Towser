using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Towser
{
    public class TowserHub : Hub<ITerminal>
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        public override Task OnConnected()
        {
            var connectionId = Context.ConnectionId;

            var encodingName = WebConfigurationManager.AppSettings["encoding"];
            var altEncodingName = WebConfigurationManager.AppSettings["altencoding"];

            try
            {
                _tcm.Connect(connectionId);
                var emu = new BasicEmulation(Clients.Caller, encodingName, altEncodingName);
                Task.Factory.StartNew(() => _tcm.ReadLoop(connectionId, emu));
            }
            catch (Exception e)
            {
                Clients.Caller.Error("Initialisation error\n" + e);
            }

            return base.OnConnected();
        }

        public void KeyPress(string data)
        {
            _tcm.Write(Context.ConnectionId, data);
        }
    }
}
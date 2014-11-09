using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Towser
{
    public class TowserHub : Hub
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        public override Task OnConnected()
        {
            var connectionId = Context.ConnectionId;

            try
            {
                _tcm.Connect(connectionId);

                Action<string> sendAction = (s) => Clients.Caller.write(s);
                Action disconnectAction = () => _tcm.Disconnect(connectionId);

                Task.Factory.StartNew(() => _tcm.ReadLoop(connectionId, sendAction, disconnectAction));
            }
            catch (Exception e)
            {
                Clients.Caller.error("Initialisation error\n" + e);
            }

            return base.OnConnected();
        }

        public void KeyPress(string data)
        {
            _tcm.Action(Context.ConnectionId, (c => c.Write(data)));
        }
    }
}
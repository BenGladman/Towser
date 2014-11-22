using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Towser
{
    public class TowserPersistentConnection : PersistentConnection
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            Action<string> writeToTerminal = (s) => Connection.Send(connectionId, s);
            var emu = new BaseEmulation(writeToTerminal);
            return _tcm.Init(connectionId, emu);
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            _tcm.Write(connectionId, data);
            return base.OnReceived(request, connectionId, data);
        }

        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            _tcm.Disconnect(connectionId);
            return base.OnDisconnected(request, connectionId, stopCalled);
        }
    }
}
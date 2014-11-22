using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace Towser
{
    public class TowserHub : Hub<ITerminal>
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        public override Task OnConnected()
        {
            var connectionId = Context.ConnectionId;
            var emu = new Vt100Emulation(Clients.Caller);
            return _tcm.Init(connectionId, emu);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _tcm.Disconnect(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public void KeyPress(string data)
        {
            _tcm.Write(Context.ConnectionId, data);
        }
    }
}
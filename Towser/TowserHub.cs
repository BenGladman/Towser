using System.Web.Hosting;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace Towser
{
    public class TowserHub : Hub<ITerminal>
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        public override async Task OnConnected()
        {
            var connectionId = Context.ConnectionId;
            var emu = new Vt100Emulation(Clients.Caller);
            await _tcm.Init(connectionId, emu);
            HostingEnvironment.QueueBackgroundWorkItem((ct) => _tcm.ReadLoop(connectionId, emu));
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _tcm.Disconnect(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public async Task KeyPress(string data)
        {
            await _tcm.Write(Context.ConnectionId, data);
        }
    }
}
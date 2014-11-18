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
        }

<<<<<<< HEAD
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
=======
        public override async Task OnDisconnected(bool stopCalled)
        {
            await _tcm.Disconnect(Context.ConnectionId);
>>>>>>> Use SignalR Hub
        }

        public async Task KeyPress(string data)
        {
            await _tcm.Write(Context.ConnectionId, data);
        }
    }
}
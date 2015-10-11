using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Towser.Hub
{
    /// <summary>
    /// Manages comms between Telnet server and SignalR hub clients.
    /// </summary>
    public class TowserHub : Hub<ITerminal>
    {
        private static Telnet.ClientManager _tcm = new Telnet.ClientManager();

        public override async Task OnConnected()
        {
            var connectionId = Context.ConnectionId;
            var decoder = new Decoder(Clients.Caller);
            await _tcm.Init(connectionId, decoder);
            HostingEnvironment.QueueBackgroundWorkItem(async (ct) =>
            {
                await _tcm.ReadLoop(connectionId, decoder, ct);
                await Clients.Caller.Stop();
            });
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
using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Towser.Vanilla
{
    /// <summary>
    /// Manages comms between Telnet Server and SignalR Persistent Connection clients with minimal processing.
    /// </summary>
    public class VanillaConnection : PersistentConnection
    {
        private static Telnet.ClientManager _tcm = new Telnet.ClientManager();

        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            Func<string, Task> writeToTerminal = (s) => Connection.Send(connectionId, s);
            var decoder = new Decoder(writeToTerminal);
            await _tcm.Init(connectionId, decoder);
            HostingEnvironment.QueueBackgroundWorkItem((ct) => _tcm.ReadLoop(connectionId, decoder));
        }

        protected override async Task OnReceived(IRequest request, string connectionId, string data)
        {
            await _tcm.Write(connectionId, data);
        }

        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            _tcm.Disconnect(connectionId);
            return base.OnDisconnected(request, connectionId, stopCalled);
        }
    }
}
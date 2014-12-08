using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Towser
{
    public class TowserPersistentConnection : PersistentConnection
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            Func<string, Task> writeToTerminal = (s) => Connection.Send(connectionId, s);
            var decoder = new TermDecoder(writeToTerminal);
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
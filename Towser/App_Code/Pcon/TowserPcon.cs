using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;
using System.Web.Hosting;
  
namespace Towser.Pcon
{
    /// <summary>
    /// Manages comms between Telnet Server and SignalR Persistent Connection clients with minimal processing.
    /// </summary>
    public class TowserPcon : PersistentConnection
    {
        private static Telnet.ClientManager _tcm = new Telnet.ClientManager();

        protected override async Task OnConnected(IRequest request, string connectionId)
        {
            var decoder = new Decoder(connectionId);
            await _tcm.Init(connectionId, decoder);
            HostingEnvironment.QueueBackgroundWorkItem((ct) => _tcm.ReadLoop(connectionId, decoder, ct));
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
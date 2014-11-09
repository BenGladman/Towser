using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;

namespace Towser
{
    public class TowserPersistentConnection : PersistentConnection
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            try
            {
                _tcm.Connect(connectionId);

                Action<string> sendAction = (s) => Connection.Send(connectionId, s);
                Action disconnectAction = () => _tcm.Disconnect(connectionId);

                Task.Factory.StartNew(() => _tcm.ReadLoop(connectionId, sendAction, disconnectAction));

                return null;
            }
            catch (Exception e)
            {
                return Connection.Send(connectionId, "Initialisation error\n" + e);
            }
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            _tcm.Action(connectionId, (c => c.Write(data)));
            return base.OnReceived(request, connectionId, data);
        }

        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            _tcm.Disconnect(connectionId);
            return base.OnDisconnected(request, connectionId, stopCalled);
        }
    }
}
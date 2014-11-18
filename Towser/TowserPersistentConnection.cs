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
            var encodingName = WebConfigurationManager.AppSettings["encoding"];
            var altEncodingName = WebConfigurationManager.AppSettings["altencoding"];

            try
            {
                _tcm.Connect(connectionId);

                var term = new PersistentConnectionTerminal();

                var emu = new BasicEmulation(term, encodingName, altEncodingName);
                Task.Factory.StartNew(() => _tcm.ReadLoop(connectionId, emu));

                return null;
            }
            catch (Exception e)
            {
                return Connection.Send(connectionId, "Initialisation error\n" + e);
            }
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

        private class PersistentConnectionTerminal : ITerminal
        {
            public void Write(string s) { /* TODO */ }
            public void Error(string s) { /* TODO */ }
        }
    }
}
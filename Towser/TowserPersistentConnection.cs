using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Towser
{
    public class TowserPersistentConnection : PersistentConnection
    {
        private static TelnetClientManager _tcm = new TelnetClientManager();

        protected override async Task OnConnected(IRequest request, string connectionId)
        {
<<<<<<< HEAD
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
=======
            Action<string> writeToTerminal = (s) => Connection.Send(connectionId, s);
            var emu = new BaseEmulation(writeToTerminal);
            await _tcm.Init(connectionId, emu);
>>>>>>> Use SignalR Hub
        }

        protected override async Task OnReceived(IRequest request, string connectionId, string data)
        {
            await _tcm.Write(connectionId, data);
        }

        protected override async Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            await _tcm.Disconnect(connectionId);
        }

        private class PersistentConnectionTerminal : ITerminal
        {
            public void Write(string s) { /* TODO */ }
            public void Error(string s) { /* TODO */ }
        }
    }
}
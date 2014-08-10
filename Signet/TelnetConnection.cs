using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Signet
{
    public class TelnetConnection : PersistentConnection
    {
        private static Dictionary<string, TelnetClient> _clients = new Dictionary<string, TelnetClient>();

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            try
            {
                var server = WebConfigurationManager.AppSettings["server"];
                var port = Int32.Parse(WebConfigurationManager.AppSettings["port"]);
                var encodingName = WebConfigurationManager.AppSettings["encoding"];

                var client = new TelnetClient(server, port, encodingName);
                _clients[connectionId] = client;

                Task.Factory.StartNew(() => SendStream(connectionId));

                return null;
            }
            catch (Exception e)
            {
                return Connection.Send(connectionId, "Initialisation error\n" + e);
            }
        }

        private void SendStream(string connectionId)
        {
            var client = GetClient(connectionId);
            if (client != null)
            {
                while (client.IsConnected)
                {
                    var str = client.Read();
                    if (!String.IsNullOrEmpty(str))
                    {
                        Connection.Send(connectionId, str);
                    }
                }

                Disconnect(connectionId);
            }
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            var client = GetClient(connectionId);
            if (client != null)
            {
                client.Write(data);
            }

            return base.OnReceived(request, connectionId, data);
        }

        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            Disconnect(connectionId);
            return base.OnDisconnected(request, connectionId, stopCalled);
        }

        private void Disconnect(string connectionId)
        {
            var client = GetClient(connectionId);
            if (client != null)
            {
                client.Disconnect();
                _clients.Remove(connectionId);
            }
        }

        /// <summary>
        /// Returns the TelnetClient associated with the connectionId, or null
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        private TelnetClient GetClient(string connectionId)
        {
            TelnetClient client = null;
            _clients.TryGetValue(connectionId, out client);
            return client;
        }
    }
}
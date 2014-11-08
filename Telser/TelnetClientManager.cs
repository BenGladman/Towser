using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace Telser
{
    class TelnetClientManager
    {
        private static Dictionary<string, TelnetClient> _clients = new Dictionary<string, TelnetClient>();

        public TelnetClient Connect(string connectionId)
        {
            var server = WebConfigurationManager.AppSettings["server"];
            var port = Int32.Parse(WebConfigurationManager.AppSettings["port"]);
            var encodingName = WebConfigurationManager.AppSettings["encoding"];
            var altEncodingName = WebConfigurationManager.AppSettings["altencoding"];
            var termtype = WebConfigurationManager.AppSettings["termtype"];

            var client = new TelnetClient(server, port, encodingName, altEncodingName, termtype);
            _clients[connectionId] = client;

            return client;
        }

        public void Disconnect(string connectionId)
        {
            var client = Get(connectionId);
            if (client != null)
            {
                client.Disconnect();
                _clients.Remove(connectionId);
            }
        }

        /// <summary>
        /// Returns the TelnetClient associated with the connectionId, or null.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public TelnetClient Get(string connectionId)
        {
            TelnetClient client = null;
            _clients.TryGetValue(connectionId, out client);
            return client;
        }

        /// <summary>
        /// Perform an action using a TelnetClient.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="action"></param>
        public void Action(string connectionId, Action<TelnetClient> action)
        {
            var client = Get(connectionId);
            if (client != null)
            {
                action(client);
            }
        }

        /// <summary>
        /// Run the read loop on the TelnetClient.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="sendAction"></param>
        /// <param name="disconnectAction"></param>
        public void ReadLoop(string connectionId, Action<string> sendAction, Action disconnectAction)
        {
            var client = Get(connectionId);
            if (client != null)
            {
                var loginPrompt = WebConfigurationManager.AppSettings["loginPrompt"];
                var login = WebConfigurationManager.AppSettings["login"];
                var passwordPrompt = WebConfigurationManager.AppSettings["passwordPrompt"];
                var password = WebConfigurationManager.AppSettings["password"];

                client.ReadLoop(sendAction, disconnectAction, loginPrompt, login, passwordPrompt, password);
            }
        }
    }
}
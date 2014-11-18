using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;

namespace Towser
{
    class TelnetClientManager
    {
        private Dictionary<string, TelnetClient> _clients = new Dictionary<string, TelnetClient>();

        private Encoding _encoding;

        public TelnetClientManager()
        {
            var encodingName = WebConfigurationManager.AppSettings["encoding"];
            _encoding = Encoding.GetEncoding(encodingName);
        }

        public TelnetClient Connect(string connectionId)
        {
            var server = WebConfigurationManager.AppSettings["server"];
            var port = Int32.Parse(WebConfigurationManager.AppSettings["port"]);
            var termtype = WebConfigurationManager.AppSettings["termtype"];

            var client = new TelnetClient(server, port, termtype);
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
        public TelnetClient Get(string connectionId)
        {
            TelnetClient client = null;
            _clients.TryGetValue(connectionId, out client);
            return client;
        }

        /// <summary>
        /// Write a string to the TelnetClient.
        /// </summary>
        public void Write(string connectionId, string data)
        {
            var client = Get(connectionId);
            if (client != null)
            {
                Write(client, data);
            }
        }

        private void Write(TelnetClient client, string data)
        {
            var bytes = _encoding.GetBytes(data);
            client.Write(bytes);
        }

        public void ReadLoop(string connectionId, ITerminalEmulation emu)
        {
            var client = Get(connectionId);
            if (client == null) { return; }

            var loginPrompt = WebConfigurationManager.AppSettings["loginPrompt"];
            var login = WebConfigurationManager.AppSettings["login"];
            var passwordPrompt = WebConfigurationManager.AppSettings["passwordPrompt"];
            var password = WebConfigurationManager.AppSettings["password"];

            var loginAuto = (!String.IsNullOrEmpty(loginPrompt) && !String.IsNullOrEmpty(login));
            var passwordAuto = (!String.IsNullOrEmpty(passwordPrompt) && !String.IsNullOrEmpty(password));

            emu.ScriptFunc = delegate(string str)
            {
                if (!String.IsNullOrEmpty(str))
                {
                    if (loginAuto && str.EndsWith(loginPrompt, StringComparison.Ordinal))
                    {
                        Write(client, login + "\r\n");
                        loginAuto = false;
                        str = str.Remove(str.Length - loginPrompt.Length);
                    }

                    if (passwordAuto && str.EndsWith(passwordPrompt, StringComparison.Ordinal))
                    {
                        Write(client, password + "\r\n");
                        passwordAuto = false;
                        str = str.Remove(str.Length - passwordPrompt.Length);
                    }
                }
                return str;
            };

            const int bufferSize = 1024;

            while (client.IsConnected)
            {
                var inBytes = client.Read(bufferSize);

                foreach (var b in inBytes)
                {
                    emu.AddByte(b);
                }
                emu.Flush();
            }

            Disconnect(connectionId);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Towser
{
    class TelnetClientManager
    {
        private Dictionary<string, TelnetClient> _clients = new Dictionary<string, TelnetClient>();

        private async Task<TelnetClient> Connect(string connectionId, string server, int port, string termtype, string encodingName)
        {
            var client = new TelnetClient(server, port, termtype, encodingName);
            _clients[connectionId] = client;

            await client.ConnectAsync();

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
            return;
        }

        /// <summary>
        /// Returns the TelnetClient associated with the connectionId, or null.
        /// </summary>
        private TelnetClient Get(string connectionId)
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
                client.Write(data);
            }
        }

        /// <summary>
        /// Initialise a new telnet connection based on the web config.
        /// </summary>
        public async Task Init(string connectionId, BaseEmulation emu)
        {
            var server = WebConfigurationManager.AppSettings["server"];
            var port = Int32.Parse(WebConfigurationManager.AppSettings["port"]);
            var termtype = WebConfigurationManager.AppSettings["termtype"];
            var encodingName = WebConfigurationManager.AppSettings["encoding"];
            var altEncodingName = WebConfigurationManager.AppSettings["altencoding"];

            emu.SetEncoding(encodingName, altEncodingName);

            var client = await Connect(connectionId, server, port, termtype, encodingName);

            // don't await the looptask, as it runs indefinitely
            var looptask = Task.Run(() => ReadLoop(connectionId, emu));

        }

        /// <summary>
        /// Wait for data from the telnet server and send it to the emulation.
        /// </summary>
        private async Task ReadLoop(string connectionId, BaseEmulation emu)
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
                        client.Write(login + "\r\n");
                        loginAuto = false;
                        str = str.Remove(str.Length - loginPrompt.Length);
                    }

                    if (passwordAuto && str.EndsWith(passwordPrompt, StringComparison.Ordinal))
                    {
                        client.Write(password + "\r\n");
                        passwordAuto = false;
                        str = str.Remove(str.Length - passwordPrompt.Length);
                    }
                }
                return str;
            };

            const int bufferSize = 1024;

            while (client.IsConnected)
            {
                var inBytes = await client.ReadAsync(bufferSize);

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
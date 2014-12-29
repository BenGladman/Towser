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
            var client = new TelnetClient();
            await client.ConnectAsync(server, port, termtype, encodingName);
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
        public async Task Write(string connectionId, string data)
        {
            var client = Get(connectionId);
            if (client != null)
            {
                await client.StreamWriter.WriteAsync(data);
            }
        }

        /// <summary>
        /// Initialise a new telnet connection based on the web config.
        /// </summary>
        public async Task Init(string connectionId, TermDecoder decoder)
        {
            var server = WebConfigurationManager.AppSettings["server"];
            var port = Int32.Parse(WebConfigurationManager.AppSettings["port"]);
            var termtype = WebConfigurationManager.AppSettings["termtype"];
            var encodingName = WebConfigurationManager.AppSettings["encoding"];
            var altEncodingName = WebConfigurationManager.AppSettings["altencoding"];

            decoder.SetEncoding(encodingName, altEncodingName);

            var client = await Connect(connectionId, server, port, termtype, encodingName);
        }

        /// <summary>
        /// Wait for data from the telnet server and send it to the emulation.
        /// </summary>
        public async Task ReadLoop(string connectionId, TermDecoder decoder)
        {
            var client = Get(connectionId);
            if (client == null) { return; }

            var loginPrompt = WebConfigurationManager.AppSettings["loginPrompt"];
            var login = WebConfigurationManager.AppSettings["login"];
            var passwordPrompt = WebConfigurationManager.AppSettings["passwordPrompt"];
            var password = WebConfigurationManager.AppSettings["password"];

            var loginAuto = (!String.IsNullOrEmpty(loginPrompt) && !String.IsNullOrEmpty(login));
            var passwordAuto = (!String.IsNullOrEmpty(passwordPrompt) && !String.IsNullOrEmpty(password));

            decoder.ScriptFunc = async (string str) =>
                {
                    if (!String.IsNullOrEmpty(str))
                    {
                        if (loginAuto && str.EndsWith(loginPrompt, StringComparison.Ordinal))
                        {
                            await client.StreamWriter.WriteAsync(login + "\r\n");
                            loginAuto = false;
                            str = str.Remove(str.Length - loginPrompt.Length);
                        }

                        if (passwordAuto && str.EndsWith(passwordPrompt, StringComparison.Ordinal))
                        {
                            await client.StreamWriter.WriteAsync(password + "\r\n");
                            passwordAuto = false;
                            str = str.Remove(str.Length - passwordPrompt.Length);
                        }
                    }
                    return str;
                };

            const int bufferSize = 4096;

            while (client.IsConnected)
            {
                var inBytes = await client.ReadAsync(bufferSize);

                foreach (var b in inBytes)
                {
                    await decoder.AddByte(b);
                }
                await decoder.Flush();
            }

            Disconnect(connectionId);
        }
    }
}
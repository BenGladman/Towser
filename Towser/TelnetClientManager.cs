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
        private Encoding _altEncoding;

        public TelnetClientManager()
        {
            var encodingName = WebConfigurationManager.AppSettings["encoding"];
            var altEncodingName = WebConfigurationManager.AppSettings["altencoding"];
            _encoding = Encoding.GetEncoding(encodingName);
            _altEncoding = Encoding.GetEncoding(altEncodingName);
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

        /// <summary>
        /// Continuously read from telnet until disconnected.
        /// </summary>
        public void ReadLoop(string connectionId, Action<string> sendAction, Action disconnectAction)
        {
            var client = Get(connectionId);
            if (client == null) { return; }

            var standardDecoder = _encoding.GetDecoder();
            var altDecoder = _altEncoding.GetDecoder();
            var activeDecoder = standardDecoder;

            var loginPrompt = WebConfigurationManager.AppSettings["loginPrompt"];
            var login = WebConfigurationManager.AppSettings["login"];
            var passwordPrompt = WebConfigurationManager.AppSettings["passwordPrompt"];
            var password = WebConfigurationManager.AppSettings["password"];

            var loginAuto = (!String.IsNullOrEmpty(loginPrompt) && !String.IsNullOrEmpty(login));
            var passwordAuto = (!String.IsNullOrEmpty(passwordPrompt) && !String.IsNullOrEmpty(password));

            const int bufferSize = 1024;

            var outBytes = new byte[bufferSize];
            var bytelen = 0;
            var sb = new StringBuilder();

            Action appendBytesToSb = delegate()
            {
                if (bytelen > 0)
                {
                    var chars = new char[bytelen];
                    var charlen = activeDecoder.GetChars(outBytes, 0, bytelen, chars, 0);
                    if (charlen > 0)
                    {
                        sb.Append(chars, 0, charlen);
                    }
                    bytelen = 0;
                }
            };

            while (client.IsConnected)
            {
                var inBytes = client.Read(bufferSize);

                foreach (var b in inBytes)
                {
                    if (b == 0x0e)
                    {
                        // ascii ShiftOut character - use alternate decoder
                        appendBytesToSb();
                        activeDecoder = altDecoder;
                    }
                    else if (b == 0x0f)
                    {
                        // ascii ShiftIn character - use standard decoder
                        appendBytesToSb();
                        activeDecoder = standardDecoder;
                    }
                    else
                    {
                        // append byte to output
                        outBytes[bytelen] = b;
                        bytelen += 1;
                    }
                }

                appendBytesToSb();

                if (sb.Length > 0)
                {
                    var str = sb.ToString();
                    sb.Clear();

                    if (String.IsNullOrEmpty(str)) { continue; }

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

                    if (str.Length > 0)
                    {
                        sendAction(str);
                    }
                }
            }

            if (disconnectAction != null) { disconnectAction(); }
        }
    }
}
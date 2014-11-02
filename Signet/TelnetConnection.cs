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

        private bool _loginAuto = false;
        private string _loginPrompt;
        private string _login;
        private bool _passwordAuto = false;
        private string _passwordPrompt;
        private string _password;

        protected override Task OnConnected(IRequest request, string connectionId)
        {
            try
            {
                var server = WebConfigurationManager.AppSettings["server"];
                var port = Int32.Parse(WebConfigurationManager.AppSettings["port"]);
                var encodingName = WebConfigurationManager.AppSettings["encoding"];
                var altEncodingName = WebConfigurationManager.AppSettings["altencoding"];
                var termtype = WebConfigurationManager.AppSettings["termtype"];

                _loginPrompt = WebConfigurationManager.AppSettings["loginPrompt"];
                _login = WebConfigurationManager.AppSettings["login"];
                if (!String.IsNullOrEmpty(_loginPrompt) && !String.IsNullOrEmpty(_login)) { _loginAuto = true; }

                _passwordPrompt = WebConfigurationManager.AppSettings["passwordPrompt"];
                _password = WebConfigurationManager.AppSettings["password"];
                if (!String.IsNullOrEmpty(_passwordPrompt) && !String.IsNullOrEmpty(_password)) { _passwordAuto = true; }

                var client = new TelnetClient(server, port, encodingName, altEncodingName, termtype);
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

                    if (String.IsNullOrEmpty(str)) { continue; }

                    if (_loginAuto && str.EndsWith(_loginPrompt, StringComparison.Ordinal))
                    {
                        client.Write(_login + "\r\n");
                        _loginAuto = false;
                        str = str.Remove(str.Length - _loginPrompt.Length);
                    }

                    if (_passwordAuto && str.EndsWith(_passwordPrompt))
                    {
                        client.Write(_password + "\r\n");
                        _passwordAuto = false;
                        str = str.Remove(str.Length - _passwordPrompt.Length);
                    }

                    if (str.Length > 0)
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
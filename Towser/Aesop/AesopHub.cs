﻿using System.Web.Hosting;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace Towser.Aesop
{
    /// <summary>
    /// Manages comms between Telnet server and SignalR hub clients which understand <see cref="AesopFragment"/>s.
    /// </summary>
    public class AesopHub : Hub<ITerminal>
    {
        private static Telnet.ClientManager _tcm = new Telnet.ClientManager();

        public override async Task OnConnected()
        {
            var connectionId = Context.ConnectionId;
            var decoder = new Decoder(Clients.Caller);
            await _tcm.Init(connectionId, decoder);
            HostingEnvironment.QueueBackgroundWorkItem((ct) => _tcm.ReadLoop(connectionId, decoder));
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _tcm.Disconnect(Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public async Task KeyPress(string data)
        {
            await _tcm.Write(Context.ConnectionId, data);
        }
    }
}
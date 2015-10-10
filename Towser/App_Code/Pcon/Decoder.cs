using Microsoft.AspNet.SignalR;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Towser.Pcon
{
    /// <summary>
    /// Decodes the bytes received from the server into strings and writes the strings to the terminal.
    /// </summary>
    public class Decoder : BaseDecoder
    {
        private readonly string _connectionId;

        public Decoder(string connectionId)
        {
            _connectionId = connectionId;
        }

        public override async Task Flush()
        {
            var str = await GetDecodedString();
            if (str.Length > 0)
            {
                var context = GlobalHost.ConnectionManager.GetConnectionContext<TowserPcon>();
                await context.Connection.Send(_connectionId, str);
            }
        }
    }
}
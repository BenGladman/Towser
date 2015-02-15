using Microsoft.AspNet.SignalR;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Towser.Adac
{
    /// <summary>
    /// Decodes the bytes received from the server into strings and writes the strings to the terminal.
    /// </summary>
    public class Decoder : BaseDecoder
    {
        private readonly ITerminal _terminal;

        public Decoder(ITerminal terminal)
        {
            _terminal = terminal;
        }

        public override async Task Flush()
        {
            var str = await GetDecodedString();
            if (str.Length > 0)
            {
                await _terminal.Write(str);
            }
        }
    }
}
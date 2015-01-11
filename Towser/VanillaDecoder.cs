using System;
using System.Text;
using System.Threading.Tasks;

namespace Towser
{
    /// <summary>
    /// Decodes the bytes received from the server into strings and writes the strings to the terminal.
    /// </summary>
    public class VanillaDecoder : BaseDecoder
    {
        private readonly Func<string, Task> _writeToTerminal;

        public VanillaDecoder(Func<string, Task> writeToTerminal)
        {
            _writeToTerminal = writeToTerminal;
        }

        public override async Task Flush()
        {
            var str = await GetDecodedString();
            if (str.Length > 0) { await _writeToTerminal(str); }
        }
    }
}
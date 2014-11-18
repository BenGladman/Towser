using System;
using System.Text;

namespace Towser
{
    public interface ITerminalEmulation
    {
        /// <summary>
        /// Add a byte to the buffer.
        /// </summary>
        void AddByte(byte b);

        /// <summary>
        /// Flush the buffer to the terminal.
        /// </summary>
        void Flush();

        /// <summary>
        /// Function to execute before sending the decoded string to the terminal.
        /// </summary>
        Func<string, string> ScriptFunc { set; }
    }
}

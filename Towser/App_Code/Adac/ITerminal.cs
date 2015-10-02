using System.Collections.Generic;
using System.Threading.Tasks;

namespace Towser.Adac
{
    /// <summary>
    /// A terminal that receives strings with embedded ANSI escape sequences.
    /// </summary>
    public interface ITerminal
    {
        Task Write(string data);
        Task Error(string s);

        /// <summary>
        /// Device Control String
        /// </summary>
        Task Dcs(string data);

        /// <summary>
        /// Operating System Command
        /// </summary>
        Task Osc(string data);

        /// <summary>
        /// Privacy Message
        /// </summary>
        Task Pm(string data);

        /// <summary>
        /// Application Program Command
        /// </summary>
        Task Apc(string data);
    }
}
